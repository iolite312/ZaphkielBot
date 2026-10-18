using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielShared.Domain;
using ZaphkielShared.Queries;

namespace ZaphkielCore.Queries;

public class GetAnalyticsHandler(IIncidentReadRepository incidents) : IQueryHandler<GetAnalyticsQuery, AnalyticsReport>
{
    public async Task<AnalyticsReport> Handle(GetAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (query.From > query.To)
            throw new ArgumentException("'from' must be before 'to'.");
        if (query.To - query.From > TimeSpan.FromDays(GetAnalyticsQuery.MaxRangeDays))
            throw new ArgumentException($"The range can be at most {GetAnalyticsQuery.MaxRangeDays} days.");

        var found = new List<Incident>();
        await foreach (var incident in incidents.GetCreatedBetweenAsync(query.From, query.To, cancellationToken))
            found.Add(incident);

        var bySeverity = Enum.GetValues<Severity>()
            .Select(severity => Summarize(severity, found.Where(i => i.Severity == severity).ToList()))
            .ToList();

        return new AnalyticsReport(
            query.From, query.To,
            found.Count,
            found.Count(i => i.Status == IncidentStatus.Open),
            found.Count(i => i.Status == IncidentStatus.Resolved),
            MeanMinutes(found),
            bySeverity);
    }

    private static SeverityStats Summarize(Severity severity, List<Incident> incidents) => new(
        severity,
        incidents.Count,
        incidents.Count(i => i.Status == IncidentStatus.Open),
        incidents.Count(i => i.Status == IncidentStatus.Resolved),
        MeanMinutes(incidents));

    private static double? MeanMinutes(IEnumerable<Incident> incidents)
    {
        var durations = incidents.Select(i => i.TimeToResolve?.TotalMinutes).OfType<double>().ToList();
        return durations.Count == 0 ? null : durations.Average();
    }
}

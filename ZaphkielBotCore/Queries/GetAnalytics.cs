using ZaphkielBotCore.Abstractions;
using ZaphkielBotCore.Cqrs;
using ZaphkielBotCore.Domain;

namespace ZaphkielBotCore.Queries;

public sealed record SeverityStats(
    Severity Severity,
    int Total,
    int Open,
    int Resolved,
    double? MeanTimeToResolveMinutes);

public sealed record AnalyticsReport(
    DateTimeOffset From,
    DateTimeOffset To,
    int Total,
    int Open,
    int Resolved,
    double? MeanTimeToResolveMinutes,
    IReadOnlyList<SeverityStats> BySeverity);

public sealed record GetAnalyticsQuery(DateTimeOffset From, DateTimeOffset To) : IQuery<AnalyticsReport>
{
    public const int MaxRangeDays = 366;
}

/// <summary>Severity breakdown and MTTR (mean time to resolve) for incidents created in a period.</summary>
public sealed class GetAnalyticsHandler(IIncidentReader reader) : IQueryHandler<GetAnalyticsQuery, AnalyticsReport>
{
    public async Task<AnalyticsReport> Handle(GetAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (query.From > query.To)
            throw new ArgumentException("'from' must be before 'to'.");
        if (query.To - query.From > TimeSpan.FromDays(GetAnalyticsQuery.MaxRangeDays))
            throw new ArgumentException($"The range can be at most {GetAnalyticsQuery.MaxRangeDays} days.");

        var incidents = new List<Incident>();
        await foreach (var incident in reader.GetCreatedBetweenAsync(query.From, query.To, cancellationToken))
            incidents.Add(incident);

        var bySeverity = Enum.GetValues<Severity>()
            .Select(severity => Summarize(severity, incidents.Where(i => i.Severity == severity).ToList()))
            .ToList();

        return new AnalyticsReport(
            query.From, query.To,
            incidents.Count,
            incidents.Count(i => i.Status == IncidentStatus.Open),
            incidents.Count(i => i.Status == IncidentStatus.Resolved),
            MeanMinutes(incidents),
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

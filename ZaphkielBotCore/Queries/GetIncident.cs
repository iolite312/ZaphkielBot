using ZaphkielBotCore.Abstractions;
using ZaphkielBotCore.Cqrs;
using ZaphkielBotCore.Domain;

namespace ZaphkielBotCore.Queries;

public sealed record IncidentView(
    string Id,
    string Title,
    string Description,
    Severity Severity,
    IncidentStatus Status,
    string ReportedBy,
    int? GitHubIssueNumber,
    string? GitHubIssueUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    double? TimeToResolveMinutes)
{
    public static IncidentView From(Incident i) => new(
        i.Id, i.Title, i.Description, i.Severity, i.Status, i.ReporterName,
        i.GitHubIssueNumber, i.GitHubIssueUrl, i.CreatedAt, i.ResolvedAt, i.TimeToResolve?.TotalMinutes);
}

public sealed record GetIncidentQuery(string IncidentId) : IQuery<IncidentView?>;

public sealed class GetIncidentHandler(IIncidentReader reader) : IQueryHandler<GetIncidentQuery, IncidentView?>
{
    public async Task<IncidentView?> Handle(GetIncidentQuery query, CancellationToken cancellationToken)
    {
        var incident = await reader.GetByIdAsync(query.IncidentId, cancellationToken);
        return incident is null ? null : IncidentView.From(incident);
    }
}

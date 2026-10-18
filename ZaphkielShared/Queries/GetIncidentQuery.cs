using ZaphkielShared.Models;
using ZaphkielShared.Messaging;
using ZaphkielShared.Models;

namespace ZaphkielShared.Queries;

public record GetIncidentQuery(string IncidentId) : IQuery<IncidentView?>;

public record IncidentView(
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

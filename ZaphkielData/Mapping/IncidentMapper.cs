using ZaphkielData.Entities;
using ZaphkielShared.Domain;

namespace ZaphkielData.Mapping;

public class IncidentMapper : IIncidentMapper
{
    public IncidentEntity ToEntity(Incident i) => new()
    {
        PartitionKey = IncidentKeys.PartitionKeyFor(i.CreatedAt),
        RowKey = IncidentKeys.RowKeyFor(i.CreatedAt, i.Id),
        IncidentId = i.Id,
        Title = i.Title,
        Description = i.Description,
        Severity = i.Severity.ToString(),
        Status = i.Status.ToString(),
        ReporterId = i.ReporterId,
        ReporterName = i.ReporterName,
        ChannelId = i.ChannelId,
        GitHubIssueNumber = i.GitHubIssueNumber,
        GitHubIssueUrl = i.GitHubIssueUrl,
        CreatedAt = i.CreatedAt,
        ResolvedAt = i.ResolvedAt
    };

    public Incident ToDomain(IncidentEntity e) => Incident.Rehydrate(
        e.IncidentId, e.Title, e.Description,
        Enum.Parse<Severity>(e.Severity), Enum.Parse<IncidentStatus>(e.Status),
        e.ReporterId, e.ReporterName, e.ChannelId,
        e.GitHubIssueNumber, e.GitHubIssueUrl,
        e.CreatedAt, e.ResolvedAt);

    public IncidentLookupEntity ToIdLookup(Incident i) => new()
    {
        PartitionKey = IncidentKeys.IdPartition(i.Id),
        RowKey = i.Id,
        IncidentPartitionKey = IncidentKeys.PartitionKeyFor(i.CreatedAt),
        IncidentRowKey = IncidentKeys.RowKeyFor(i.CreatedAt, i.Id)
    };

    public IncidentLookupEntity ToIssueLookup(Incident i, int issueNumber) => new()
    {
        PartitionKey = IncidentKeys.IssuePartition,
        RowKey = IncidentKeys.IssueRow(issueNumber),
        IncidentPartitionKey = IncidentKeys.PartitionKeyFor(i.CreatedAt),
        IncidentRowKey = IncidentKeys.RowKeyFor(i.CreatedAt, i.Id)
    };
}

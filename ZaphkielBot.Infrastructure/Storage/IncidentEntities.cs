using System.Globalization;
using Azure;
using Azure.Data.Tables;
using ZaphkielBot.Core.Domain;

namespace ZaphkielBot.Infrastructure.Storage;

public static class TableNames
{
    public const string Incidents = "Incidents";
    public const string IncidentLookup = "IncidentLookup";
}

/// <summary>
/// Key design for the Incidents table:
///   PartitionKey = creation month (yyyyMM)        -> analytics read one partition per month, writes spread over time
///   RowKey       = reversed ticks + "_" + id      -> newest first, unique, and range-queryable by creation time
/// </summary>
public sealed class IncidentEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string IncidentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReporterId { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public int? GitHubIssueNumber { get; set; }
    public string? GitHubIssueUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public static string PartitionKeyFor(DateTimeOffset createdAt) =>
        createdAt.UtcDateTime.ToString("yyyyMM", CultureInfo.InvariantCulture);

    public static string RowKeyFor(DateTimeOffset createdAt, string id) =>
        $"{ReverseTicks(createdAt):D19}_{id}";

    /// <summary>Lowest/highest RowKey that can belong to an incident created at the given instant, regardless of id.</summary>
    public static string RowKeyLowerBound(DateTimeOffset createdAt) => $"{ReverseTicks(createdAt):D19}";
    public static string RowKeyUpperBound(DateTimeOffset createdAt) => $"{ReverseTicks(createdAt):D19}~";

    private static long ReverseTicks(DateTimeOffset at) => DateTime.MaxValue.Ticks - at.UtcTicks;

    public static IncidentEntity From(Incident i) => new()
    {
        PartitionKey = PartitionKeyFor(i.CreatedAt),
        RowKey = RowKeyFor(i.CreatedAt, i.Id),
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

    public Incident ToDomain() => Incident.Rehydrate(
        IncidentId, Title, Description,
        Enum.Parse<Severity>(Severity), Enum.Parse<IncidentStatus>(Status),
        ReporterId, ReporterName, ChannelId,
        GitHubIssueNumber, GitHubIssueUrl,
        CreatedAt, ResolvedAt);
}

/// <summary>
/// Secondary index so an incident can be found without knowing its creation time:
///   by incident id    -> PartitionKey "id-{first two chars}" (up to 256 partitions), RowKey = id
///   by GitHub issue   -> PartitionKey "gh",                    RowKey = zero padded issue number
/// </summary>
public sealed class IncidentLookupEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string IncidentPartitionKey { get; set; } = string.Empty;
    public string IncidentRowKey { get; set; } = string.Empty;

    public static IncidentLookupEntity ById(Incident i) => new()
    {
        PartitionKey = IdPartition(i.Id),
        RowKey = i.Id,
        IncidentPartitionKey = IncidentEntity.PartitionKeyFor(i.CreatedAt),
        IncidentRowKey = IncidentEntity.RowKeyFor(i.CreatedAt, i.Id)
    };

    public static IncidentLookupEntity ByIssue(Incident i, int issueNumber) => new()
    {
        PartitionKey = IssuePartition,
        RowKey = IssueRow(issueNumber),
        IncidentPartitionKey = IncidentEntity.PartitionKeyFor(i.CreatedAt),
        IncidentRowKey = IncidentEntity.RowKeyFor(i.CreatedAt, i.Id)
    };

    public const string IssuePartition = "gh";
    public static string IdPartition(string id) => $"id-{id[..2]}";
    public static string IssueRow(int issueNumber) => issueNumber.ToString("D10", CultureInfo.InvariantCulture);
}

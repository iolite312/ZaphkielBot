using System.Globalization;

namespace ZaphkielData.Entities;

public static class TableNames
{
    public const string Incidents = "Incidents";
    public const string IncidentLookup = "IncidentLookup";
}

/// <summary>
/// Key design for the tables.
/// Incidents:
///   PartitionKey = creation month (yyyyMM)        -> analytics read one partition per month, writes spread over time
///   RowKey       = reversed ticks + "_" + id      -> newest first, unique, and range-queryable by creation time
/// IncidentLookup (secondary index, so an incident can be found without knowing its creation time):
///   by incident id    -> PartitionKey "id-{first two chars}" (up to 256 partitions), RowKey = id
///   by GitHub issue   -> PartitionKey "gh",                    RowKey = zero padded issue number
/// </summary>
public static class IncidentKeys
{
    public const string IssuePartition = "gh";

    public static string PartitionKeyFor(DateTimeOffset createdAt) =>
        createdAt.UtcDateTime.ToString("yyyyMM", CultureInfo.InvariantCulture);

    public static string RowKeyFor(DateTimeOffset createdAt, string id) =>
        $"{ReverseTicks(createdAt):D19}_{id}";

    /// <summary>Lowest/highest RowKey that can belong to an incident created at the given instant, regardless of id.</summary>
    public static string RowKeyLowerBound(DateTimeOffset createdAt) => $"{ReverseTicks(createdAt):D19}";

    public static string RowKeyUpperBound(DateTimeOffset createdAt) => $"{ReverseTicks(createdAt):D19}~";

    public static string IdPartition(string id) => $"id-{id[..2]}";

    public static string IssueRow(int issueNumber) => issueNumber.ToString("D10", CultureInfo.InvariantCulture);

    private static long ReverseTicks(DateTimeOffset at) => DateTime.MaxValue.Ticks - at.UtcTicks;
}

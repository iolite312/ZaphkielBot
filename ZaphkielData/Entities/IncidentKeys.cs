using System.Globalization;

namespace ZaphkielData.Entities;

public static class TableNames
{
    public const string Incidents = "Incidents";
    public const string IncidentLookup = "IncidentLookup";
}

public static class IncidentKeys
{
    public const string IssuePartition = "gh";

    public static string PartitionKeyFor(DateTimeOffset createdAt) =>
        createdAt.UtcDateTime.ToString("yyyyMM", CultureInfo.InvariantCulture);

    public static string RowKeyFor(DateTimeOffset createdAt, string id) =>
        $"{ReverseTicks(createdAt):D19}_{id}";

    public static string RowKeyLowerBound(DateTimeOffset createdAt) => $"{ReverseTicks(createdAt):D19}";

    public static string RowKeyUpperBound(DateTimeOffset createdAt) => $"{ReverseTicks(createdAt):D19}~";

    public static string IdPartition(string id) => $"id-{id[..2]}";

    public static string IssueRow(int issueNumber) => issueNumber.ToString("D10", CultureInfo.InvariantCulture);

    private static long ReverseTicks(DateTimeOffset at) => DateTime.MaxValue.Ticks - at.UtcTicks;
}

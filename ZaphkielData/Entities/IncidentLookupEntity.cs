using Azure;
using Azure.Data.Tables;

namespace ZaphkielData.Entities;

/// <summary>Index row that points at the real <see cref="IncidentEntity"/> row.</summary>
public sealed class IncidentLookupEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string IncidentPartitionKey { get; set; } = string.Empty;
    public string IncidentRowKey { get; set; } = string.Empty;
}

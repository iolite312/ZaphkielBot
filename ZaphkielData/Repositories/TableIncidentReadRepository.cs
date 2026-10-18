using System.Runtime.CompilerServices;
using Azure.Data.Tables;
using ZaphkielCore.Abstractions;
using ZaphkielData.Entities;
using ZaphkielData.Mapping;
using ZaphkielShared.Domain;

namespace ZaphkielData.Repositories;

public class TableIncidentReadRepository(TableServiceClient tables, IIncidentMapper mapper) : IIncidentReadRepository
{
    private readonly TableClient _incidents = tables.GetTableClient(TableNames.Incidents);
    private readonly TableClient _lookup = tables.GetTableClient(TableNames.IncidentLookup);

    public Task<Incident?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        ResolveAsync(IncidentKeys.IdPartition(id), id, cancellationToken);

    public Task<Incident?> GetByGitHubIssueAsync(int issueNumber, CancellationToken cancellationToken) =>
        ResolveAsync(IncidentKeys.IssuePartition, IncidentKeys.IssueRow(issueNumber), cancellationToken);

    public async IAsyncEnumerable<Incident> GetCreatedBetweenAsync(
        DateTimeOffset from, DateTimeOffset to, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // One partition per month; inside a partition the RowKey range limits the scan to the requested period.
        // RowKeys use reversed ticks, so the later instant gives the lower bound.
        var lower = IncidentKeys.RowKeyLowerBound(to);
        var upper = IncidentKeys.RowKeyUpperBound(from);

        var utcFrom = from.UtcDateTime;
        var utcTo = to.UtcDateTime;
        for (var month = new DateTime(utcFrom.Year, utcFrom.Month, 1, 0, 0, 0, DateTimeKind.Utc);
             month <= utcTo;
             month = month.AddMonths(1))
        {
            var partition = IncidentKeys.PartitionKeyFor(month);
            var query = _incidents.QueryAsync<IncidentEntity>(
                TableClient.CreateQueryFilter($"PartitionKey eq {partition} and RowKey ge {lower} and RowKey le {upper}"),
                cancellationToken: cancellationToken);

            await foreach (var entity in query)
                yield return mapper.ToDomain(entity);
        }
    }

    private async Task<Incident?> ResolveAsync(string lookupPartition, string lookupRow, CancellationToken ct)
    {
        var lookup = await _lookup.GetEntityIfExistsAsync<IncidentLookupEntity>(lookupPartition, lookupRow, cancellationToken: ct);
        if (!lookup.HasValue) return null;

        var entity = await _incidents.GetEntityIfExistsAsync<IncidentEntity>(
            lookup.Value!.IncidentPartitionKey, lookup.Value.IncidentRowKey, cancellationToken: ct);
        return entity.HasValue ? mapper.ToDomain(entity.Value!) : null;
    }
}

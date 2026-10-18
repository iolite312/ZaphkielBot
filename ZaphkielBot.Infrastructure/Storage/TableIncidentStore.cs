using System.Runtime.CompilerServices;
using Azure;
using Azure.Data.Tables;
using ZaphkielBot.Core.Abstractions;
using ZaphkielBot.Core.Domain;

namespace ZaphkielBot.Infrastructure.Storage;

/// <summary>Azure Table Storage implementation of both sides of the incident datastore.</summary>
public sealed class TableIncidentStore(TableServiceClient tables) : IIncidentReader, IIncidentWriter
{
    private readonly TableClient _incidents = tables.GetTableClient(TableNames.Incidents);
    private readonly TableClient _lookup = tables.GetTableClient(TableNames.IncidentLookup);

    // ---- read side ----

    public Task<Incident?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        ResolveAsync(IncidentLookupEntity.IdPartition(id), id, cancellationToken);

    public Task<Incident?> GetByGitHubIssueAsync(int issueNumber, CancellationToken cancellationToken) =>
        ResolveAsync(IncidentLookupEntity.IssuePartition, IncidentLookupEntity.IssueRow(issueNumber), cancellationToken);

    public async IAsyncEnumerable<Incident> GetCreatedBetweenAsync(
        DateTimeOffset from, DateTimeOffset to, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // One partition per month; inside a partition the RowKey range limits the scan to the requested period.
        // RowKeys use reversed ticks, so the later instant gives the lower bound.
        var lower = IncidentEntity.RowKeyLowerBound(to);
        var upper = IncidentEntity.RowKeyUpperBound(from);

        var utcFrom = from.UtcDateTime;
        var utcTo = to.UtcDateTime;
        for (var month = new DateTime(utcFrom.Year, utcFrom.Month, 1, 0, 0, 0, DateTimeKind.Utc);
             month <= utcTo;
             month = month.AddMonths(1))
        {
            var partition = IncidentEntity.PartitionKeyFor(month);
            var query = _incidents.QueryAsync<IncidentEntity>(
                TableClient.CreateQueryFilter($"PartitionKey eq {partition} and RowKey ge {lower} and RowKey le {upper}"),
                cancellationToken: cancellationToken);

            await foreach (var entity in query)
                yield return entity.ToDomain();
        }
    }

    private async Task<Incident?> ResolveAsync(string lookupPartition, string lookupRow, CancellationToken ct)
    {
        var lookup = await _lookup.GetEntityIfExistsAsync<IncidentLookupEntity>(lookupPartition, lookupRow, cancellationToken: ct);
        if (!lookup.HasValue) return null;

        var entity = await _incidents.GetEntityIfExistsAsync<IncidentEntity>(
            lookup.Value!.IncidentPartitionKey, lookup.Value.IncidentRowKey, cancellationToken: ct);
        return entity.HasValue ? entity.Value!.ToDomain() : null;
    }

    // ---- write side ----

    public async Task AddAsync(Incident incident, CancellationToken cancellationToken)
    {
        try
        {
            await _incidents.AddEntityAsync(IncidentEntity.From(incident), cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            // Already stored by an earlier delivery of the same command.
        }

        // Written second and upserted, so a retry repairs the index if the first attempt died in between.
        await _lookup.UpsertEntityAsync(IncidentLookupEntity.ById(incident), TableUpdateMode.Replace, cancellationToken);
    }

    public async Task UpdateAsync(Incident incident, CancellationToken cancellationToken)
    {
        await _incidents.UpdateEntityAsync(IncidentEntity.From(incident), ETag.All, TableUpdateMode.Replace, cancellationToken);

        if (incident.GitHubIssueNumber is { } issueNumber)
            await _lookup.UpsertEntityAsync(IncidentLookupEntity.ByIssue(incident, issueNumber), TableUpdateMode.Replace, cancellationToken);
    }
}

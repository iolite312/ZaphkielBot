using Azure;
using Azure.Data.Tables;
using ZaphkielCore.Abstractions;
using ZaphkielData.Entities;
using ZaphkielData.Mapping;
using ZaphkielShared.Domain;

namespace ZaphkielData.Repositories;

public class TableIncidentWriteRepository(TableServiceClient tables, IIncidentMapper mapper) : IIncidentWriteRepository
{
    private readonly TableClient _incidents = tables.GetTableClient(TableNames.Incidents);
    private readonly TableClient _lookup = tables.GetTableClient(TableNames.IncidentLookup);

    public async Task AddAsync(Incident incident, CancellationToken cancellationToken)
    {
        try
        {
            await _incidents.AddEntityAsync(mapper.ToEntity(incident), cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            // Already stored by an earlier delivery of the same command.
        }

        // Written second and upserted, so a retry repairs the index if the first attempt died in between.
        await _lookup.UpsertEntityAsync(mapper.ToIdLookup(incident), TableUpdateMode.Replace, cancellationToken);
    }

    public async Task UpdateAsync(Incident incident, CancellationToken cancellationToken)
    {
        await _incidents.UpdateEntityAsync(mapper.ToEntity(incident), ETag.All, TableUpdateMode.Replace, cancellationToken);

        if (incident.GitHubIssueNumber is { } issueNumber)
            await _lookup.UpsertEntityAsync(mapper.ToIssueLookup(incident, issueNumber), TableUpdateMode.Replace, cancellationToken);
    }
}

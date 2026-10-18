using ZaphkielBot.Core.Domain;

namespace ZaphkielBot.Core.Abstractions;

/// <summary>Read side of the incident datastore. Used by queries and by handlers that need to look something up.</summary>
public interface IIncidentReader
{
    Task<Incident?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Incident?> GetByGitHubIssueAsync(int issueNumber, CancellationToken cancellationToken);

    /// <summary>Incidents created in the (inclusive) range, newest first.</summary>
    IAsyncEnumerable<Incident> GetCreatedBetweenAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}

/// <summary>Write side of the incident datastore. Only command handlers use this.</summary>
public interface IIncidentWriter
{
    /// <summary>Idempotent: adding an incident that already exists is not an error.</summary>
    Task AddAsync(Incident incident, CancellationToken cancellationToken);

    Task UpdateAsync(Incident incident, CancellationToken cancellationToken);
}

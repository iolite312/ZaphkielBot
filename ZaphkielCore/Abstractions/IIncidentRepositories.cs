using ZaphkielShared.Models;
using ZaphkielShared.Models;

namespace ZaphkielCore.Abstractions;

public interface IIncidentReadRepository
{
    Task<Incident?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Incident?> GetByGitHubIssueAsync(int issueNumber, CancellationToken cancellationToken);

    IAsyncEnumerable<Incident> GetCreatedBetweenAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}

public interface IIncidentWriteRepository
{
    Task AddAsync(Incident incident, CancellationToken cancellationToken);

    Task UpdateAsync(Incident incident, CancellationToken cancellationToken);
}

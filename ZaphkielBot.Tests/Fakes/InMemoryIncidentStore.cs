using System.Runtime.CompilerServices;
using ZaphkielBotCore.Abstractions;
using ZaphkielBotCore.Domain;

namespace ZaphkielBot.Tests.Fakes;

/// <summary>Stub datastore: lets handlers be tested without Azure Table Storage.</summary>
public sealed class InMemoryIncidentStore : IIncidentReader, IIncidentWriter
{
    private readonly Dictionary<string, Incident> _incidents = new();

    public int Updates { get; private set; }

    public Task<Incident?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_incidents.GetValueOrDefault(id));

    public Task<Incident?> GetByGitHubIssueAsync(int issueNumber, CancellationToken cancellationToken) =>
        Task.FromResult(_incidents.Values.FirstOrDefault(i => i.GitHubIssueNumber == issueNumber));

    public async IAsyncEnumerable<Incident> GetCreatedBetweenAsync(
        DateTimeOffset from, DateTimeOffset to, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var incident in _incidents.Values.Where(i => i.CreatedAt >= from && i.CreatedAt <= to))
            yield return incident;
        await Task.CompletedTask;
    }

    public Task AddAsync(Incident incident, CancellationToken cancellationToken)
    {
        _incidents.TryAdd(incident.Id, incident);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Incident incident, CancellationToken cancellationToken)
    {
        Updates++;
        _incidents[incident.Id] = incident;
        return Task.CompletedTask;
    }

    public void Seed(Incident incident) => _incidents[incident.Id] = incident;
}

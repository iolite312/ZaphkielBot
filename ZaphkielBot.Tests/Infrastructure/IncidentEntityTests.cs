using ZaphkielBot.Core.Domain;
using ZaphkielBot.Infrastructure.Storage;
using ZaphkielBot.Tests.Fakes;

namespace ZaphkielBot.Tests.Infrastructure;

public class IncidentEntityTests
{
    [Fact]
    public void Entity_round_trips_the_incident()
    {
        var incident = TestData.Incident(issue: 12);
        incident.Resolve(TestData.T0.AddMinutes(15));

        var restored = IncidentEntity.From(incident).ToDomain();

        Assert.Equal(incident.Id, restored.Id);
        Assert.Equal(incident.Severity, restored.Severity);
        Assert.Equal(IncidentStatus.Resolved, restored.Status);
        Assert.Equal(12, restored.GitHubIssueNumber);
        Assert.Equal(TimeSpan.FromMinutes(15), restored.TimeToResolve);
    }

    [Fact]
    public void Partition_is_the_creation_month()
    {
        Assert.Equal("202610", IncidentEntity.PartitionKeyFor(TestData.T0));
        Assert.Equal("202601", IncidentEntity.PartitionKeyFor(new DateTimeOffset(2026, 1, 31, 23, 59, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Newer_incidents_sort_first_within_a_partition()
    {
        var older = IncidentEntity.RowKeyFor(TestData.T0, "aaaaaaaa");
        var newer = IncidentEntity.RowKeyFor(TestData.T0.AddMinutes(1), "bbbbbbbb");

        Assert.True(string.CompareOrdinal(newer, older) < 0);
    }

    [Fact]
    public void Row_key_range_brackets_exactly_the_requested_period()
    {
        var from = TestData.T0;
        var to = TestData.T0.AddHours(2);
        var lower = IncidentEntity.RowKeyLowerBound(to);
        var upper = IncidentEntity.RowKeyUpperBound(from);

        bool InRange(string rowKey) => string.CompareOrdinal(rowKey, lower) >= 0 && string.CompareOrdinal(rowKey, upper) <= 0;

        Assert.True(InRange(IncidentEntity.RowKeyFor(from, "ffffffff")));      // start boundary, any id
        Assert.True(InRange(IncidentEntity.RowKeyFor(to, "00000000")));        // end boundary, any id
        Assert.True(InRange(IncidentEntity.RowKeyFor(from.AddHours(1), "abcdef12")));
        Assert.False(InRange(IncidentEntity.RowKeyFor(from.AddSeconds(-1), "abcdef12")));
        Assert.False(InRange(IncidentEntity.RowKeyFor(to.AddSeconds(1), "abcdef12")));
    }

    [Fact]
    public void Lookup_entities_point_back_at_the_incident_row()
    {
        var incident = TestData.Incident(issue: 5);

        var byId = IncidentLookupEntity.ById(incident);
        var byIssue = IncidentLookupEntity.ByIssue(incident, 5);

        Assert.Equal("id-a1", byId.PartitionKey);
        Assert.Equal("0000000005", byIssue.RowKey);
        Assert.Equal(IncidentEntity.RowKeyFor(incident.CreatedAt, incident.Id), byId.IncidentRowKey);
        Assert.Equal(byId.IncidentPartitionKey, byIssue.IncidentPartitionKey);
    }
}

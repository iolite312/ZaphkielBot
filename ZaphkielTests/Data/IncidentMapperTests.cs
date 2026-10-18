using ZaphkielData.Entities;
using ZaphkielData.Mapping;
using ZaphkielShared.Domain;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Data;

public class IncidentMapperTests
{
    private readonly IIncidentMapper _mapper = new IncidentMapper();

    [Fact]
    public void Entity_round_trips_the_incident()
    {
        var incident = TestData.Incident(issue: 12);
        incident.Resolve(TestData.T0.AddMinutes(15));

        var restored = _mapper.ToDomain(_mapper.ToEntity(incident));

        Assert.Equal(incident.Id, restored.Id);
        Assert.Equal(incident.Severity, restored.Severity);
        Assert.Equal(IncidentStatus.Resolved, restored.Status);
        Assert.Equal(12, restored.GitHubIssueNumber);
        Assert.Equal(TimeSpan.FromMinutes(15), restored.TimeToResolve);
    }

    [Fact]
    public void Entity_keys_follow_the_documented_design()
    {
        var entity = _mapper.ToEntity(TestData.Incident());

        Assert.Equal("202610", entity.PartitionKey);
        Assert.Equal(IncidentKeys.RowKeyFor(TestData.T0, "a1b2c3d4"), entity.RowKey);
    }

    [Fact]
    public void Lookup_entities_point_back_at_the_incident_row()
    {
        var incident = TestData.Incident(issue: 5);

        var byId = _mapper.ToIdLookup(incident);
        var byIssue = _mapper.ToIssueLookup(incident, 5);

        Assert.Equal("id-a1", byId.PartitionKey);
        Assert.Equal("a1b2c3d4", byId.RowKey);
        Assert.Equal("gh", byIssue.PartitionKey);
        Assert.Equal("0000000005", byIssue.RowKey);
        Assert.Equal(IncidentKeys.RowKeyFor(incident.CreatedAt, incident.Id), byId.IncidentRowKey);
        Assert.Equal(byId.IncidentPartitionKey, byIssue.IncidentPartitionKey);
        Assert.Equal(byId.IncidentRowKey, byIssue.IncidentRowKey);
    }
}

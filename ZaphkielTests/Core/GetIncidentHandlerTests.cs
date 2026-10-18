using NSubstitute;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Queries;
using ZaphkielShared.Domain;
using ZaphkielShared.Queries;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Core;

public class GetIncidentHandlerTests
{
    [Fact]
    public async Task Maps_the_incident_to_a_view_including_time_to_resolve()
    {
        var incident = TestData.Incident(issue: 7);
        incident.Resolve(TestData.T0.AddMinutes(45));
        var repository = Substitute.For<IIncidentReadRepository>();
        repository.GetByIdAsync("a1b2c3d4", Arg.Any<CancellationToken>()).Returns(incident);

        var view = await new GetIncidentHandler(repository).Handle(new GetIncidentQuery("a1b2c3d4"), CancellationToken.None);

        Assert.NotNull(view);
        Assert.Equal(IncidentStatus.Resolved, view.Status);
        Assert.Equal(45, view.TimeToResolveMinutes);
        Assert.Equal(7, view.GitHubIssueNumber);
    }

    [Fact]
    public async Task Unknown_incident_gives_null()
    {
        var repository = Substitute.For<IIncidentReadRepository>();

        Assert.Null(await new GetIncidentHandler(repository).Handle(new GetIncidentQuery("deadbeef"), CancellationToken.None));
    }
}

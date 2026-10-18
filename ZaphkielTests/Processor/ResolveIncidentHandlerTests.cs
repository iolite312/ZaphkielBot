using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZaphkielCore.Abstractions;
using ZaphkielProcessor.Handlers;
using ZaphkielShared.Commands;
using ZaphkielShared.Domain;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Processor;

public class ResolveIncidentHandlerTests
{
    private readonly InMemoryIncidentRepository _repository = new();
    private readonly IGitHubIssueClient _gitHub = Substitute.For<IGitHubIssueClient>();
    private readonly IDiscordNotifier _discord = Substitute.For<IDiscordNotifier>();
    private readonly ResolveIncidentHandler _handler;

    public ResolveIncidentHandlerTests()
    {
        _repository.Seed(TestData.Incident(issue: 7));
        _handler = new ResolveIncidentHandler(_repository, _repository, _gitHub, _discord, NullLogger<ResolveIncidentHandler>.Instance);
    }

    [Fact]
    public async Task Discord_resolve_stores_time_to_resolve_and_closes_the_github_issue()
    {
        var command = new ResolveIncidentCommand("a1b2c3d4", null, "Bob", ResolutionSource.Discord, TestData.T0.AddMinutes(90));

        await _handler.Handle(command, CancellationToken.None);

        var incident = (await _repository.GetByIdAsync("a1b2c3d4", CancellationToken.None))!;
        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal(TimeSpan.FromMinutes(90), incident.TimeToResolve);
        await _gitHub.Received(1).CloseIssueAsync(7, Arg.Any<CancellationToken>());
        await _discord.Received(1).PostMessageAsync("channel-1", Arg.Is<string>(m => m.Contains("1h 30m")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Github_close_resolves_by_issue_number_without_closing_it_again()
    {
        var command = new ResolveIncidentCommand(null, 7, "dev", ResolutionSource.GitHub, TestData.T0.AddMinutes(10));

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(IncidentStatus.Resolved, (await _repository.GetByIdAsync("a1b2c3d4", CancellationToken.None))!.Status);
        await _gitHub.DidNotReceive().CloseIssueAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolving_twice_is_a_no_op_and_keeps_the_first_resolution_time()
    {
        await _handler.Handle(new ResolveIncidentCommand("a1b2c3d4", null, "Bob", ResolutionSource.Discord, TestData.T0.AddMinutes(5)), CancellationToken.None);
        var updatesAfterFirst = _repository.Updates;

        await _handler.Handle(new ResolveIncidentCommand(null, 7, "GitHub", ResolutionSource.GitHub, TestData.T0.AddMinutes(60)), CancellationToken.None);

        Assert.Equal(updatesAfterFirst, _repository.Updates);
        Assert.Equal(TimeSpan.FromMinutes(5), (await _repository.GetByIdAsync("a1b2c3d4", CancellationToken.None))!.TimeToResolve);
        await _discord.Received(1).PostMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_issue_is_ignored()
    {
        await _handler.Handle(new ResolveIncidentCommand(null, 999, "dev", ResolutionSource.GitHub, TestData.T0), CancellationToken.None);

        await _discord.DidNotReceiveWithAnyArgs().PostMessageAsync(default!, default!, default);
    }
}

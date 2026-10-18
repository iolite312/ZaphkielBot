using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZaphkielCore.Abstractions;
using ZaphkielProcessor.Handlers;
using ZaphkielShared.Commands;
using ZaphkielShared.Domain;
using ZaphkielShared.GitHub;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Processor;

public class CreateIncidentHandlerTests
{
    private readonly InMemoryIncidentRepository _repository = new();
    private readonly IGitHubIssueClient _gitHub = Substitute.For<IGitHubIssueClient>();
    private readonly IDiscordNotifier _discord = Substitute.For<IDiscordNotifier>();
    private readonly CreateIncidentHandler _handler;

    private static readonly CreateIncidentCommand Command = new(
        "a1b2c3d4", "Database is down", "Connections time out @everyone", Severity.Critical,
        "user-1", "Alice", "channel-1", TestData.T0);

    public CreateIncidentHandlerTests()
    {
        _gitHub.CreateIssueAsync(Arg.Any<NewGitHubIssue>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubIssue(42, "https://github.com/o/r/issues/42"));
        _handler = new CreateIncidentHandler(_repository, _repository, _gitHub, _discord, NullLogger<CreateIncidentHandler>.Instance);
    }

    [Fact]
    public async Task Logs_incident_and_links_the_created_github_issue()
    {
        await _handler.Handle(Command, CancellationToken.None);

        var stored = await _repository.GetByIdAsync("a1b2c3d4", CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(Severity.Critical, stored.Severity);
        Assert.Equal(IncidentStatus.Open, stored.Status);
        Assert.Equal(42, stored.GitHubIssueNumber);
        Assert.Same(stored, await _repository.GetByGitHubIssueAsync(42, CancellationToken.None));
    }

    [Fact]
    public async Task Issue_has_incident_id_labels_and_neutralized_mentions()
    {
        await _handler.Handle(Command, CancellationToken.None);

        await _gitHub.Received(1).CreateIssueAsync(
            Arg.Is<NewGitHubIssue>(i =>
                i.Title == "[INC-a1b2c3d4] Database is down" &&
                i.Labels.SequenceEqual(new[] { "incident", "sev1" }) &&
                !i.Body.Contains("@everyone") &&
                i.Body.Contains("SEV1 - Critical")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Announces_the_issue_in_the_channel_the_incident_was_reported_in()
    {
        await _handler.Handle(Command, CancellationToken.None);

        await _discord.Received(1).PostMessageAsync(
            "channel-1", Arg.Is<string>(m => m.Contains("#42")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Redelivered_command_does_not_open_a_second_issue()
    {
        await _handler.Handle(Command, CancellationToken.None);
        await _handler.Handle(Command, CancellationToken.None);

        await _gitHub.Received(1).CreateIssueAsync(Arg.Any<NewGitHubIssue>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Github_failure_propagates_so_the_queue_retries_and_incident_stays_logged()
    {
        _gitHub.CreateIssueAsync(Arg.Any<NewGitHubIssue>(), Arg.Any<CancellationToken>())
            .Returns<GitHubIssue>(_ => throw new HttpRequestException("boom"));

        await Assert.ThrowsAsync<HttpRequestException>(() => _handler.Handle(Command, CancellationToken.None));

        Assert.NotNull(await _repository.GetByIdAsync("a1b2c3d4", CancellationToken.None));
    }

    [Fact]
    public async Task Notification_failure_after_linking_is_not_fatal()
    {
        _discord.PostMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("queue down"));

        await _handler.Handle(Command, CancellationToken.None);

        Assert.Equal(42, (await _repository.GetByIdAsync("a1b2c3d4", CancellationToken.None))!.GitHubIssueNumber);
    }
}

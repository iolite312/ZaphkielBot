using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZaphkielCore.Abstractions;
using ZaphkielProcessor.Handlers;
using ZaphkielShared.Commands;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Processor;

public class RecordGitHubCommentHandlerTests
{
    private readonly InMemoryIncidentRepository _repository = new();
    private readonly IDiscordNotifier _discord = Substitute.For<IDiscordNotifier>();
    private readonly RecordGitHubCommentHandler _handler;

    public RecordGitHubCommentHandlerTests()
    {
        _repository.Seed(TestData.Incident(issue: 7));
        _handler = new RecordGitHubCommentHandler(_repository, _discord, NullLogger<RecordGitHubCommentHandler>.Instance);
    }

    [Fact]
    public async Task Posts_the_comment_to_the_incident_channel_as_a_quote()
    {
        await _handler.Handle(new RecordGitHubCommentCommand(7, 1, "dev-jane", "Found it.\nRestarting now.", "https://github.com/o/r/issues/7#c1"), CancellationToken.None);

        await _discord.Received(1).PostMessageAsync(
            "channel-1",
            Arg.Is<string>(m => m.Contains("dev-jane") && m.Contains("INC-a1b2c3d4") && m.Contains("> Found it.\n> Restarting now.")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Long_comments_are_truncated_to_the_discord_limit()
    {
        await _handler.Handle(new RecordGitHubCommentCommand(7, 1, "dev", new string('x', 10_000), "https://example.test"), CancellationToken.None);

        await _discord.Received(1).PostMessageAsync("channel-1", Arg.Is<string>(m => m.Length <= 2000), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comments_on_issues_that_are_not_incidents_are_ignored()
    {
        await _handler.Handle(new RecordGitHubCommentCommand(404, 1, "dev", "hi", "https://example.test"), CancellationToken.None);

        await _discord.DidNotReceiveWithAnyArgs().PostMessageAsync(default!, default!, default);
    }
}

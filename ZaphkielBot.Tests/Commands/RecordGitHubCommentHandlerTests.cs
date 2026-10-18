using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZaphkielBot.Core.Abstractions;
using ZaphkielBot.Core.Commands;
using ZaphkielBot.Tests.Fakes;

namespace ZaphkielBot.Tests.Commands;

public class RecordGitHubCommentHandlerTests
{
    private readonly InMemoryIncidentStore _store = new();
    private readonly IDiscordNotifier _discord = Substitute.For<IDiscordNotifier>();
    private readonly RecordGitHubCommentHandler _handler;

    public RecordGitHubCommentHandlerTests()
    {
        _store.Seed(TestData.Incident(issue: 7));
        _handler = new RecordGitHubCommentHandler(_store, _discord, NullLogger<RecordGitHubCommentHandler>.Instance);
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

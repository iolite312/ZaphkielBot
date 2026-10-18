using NSubstitute;
using ZaphkielBot.Discord;
using ZaphkielBot.Handlers;
using ZaphkielShared.Commands;

namespace ZaphkielTests.Bot;

public class SendDiscordMessageHandlerTests
{
    [Fact]
    public async Task Hands_the_message_to_the_discord_sender()
    {
        var sender = Substitute.For<IDiscordMessageSender>();

        await new SendDiscordMessageHandler(sender).Handle(new SendDiscordMessageCommand("channel-1", "hello"), CancellationToken.None);

        await sender.Received(1).SendAsync("channel-1", "hello", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sender_failures_propagate_so_the_runtime_retries_the_queue_message()
    {
        var sender = Substitute.For<IDiscordMessageSender>();
        sender.SendAsync(default!, default!, default).ReturnsForAnyArgs(_ => throw new HttpRequestException("discord down"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            new SendDiscordMessageHandler(sender).Handle(new SendDiscordMessageCommand("c", "x"), CancellationToken.None));
    }
}

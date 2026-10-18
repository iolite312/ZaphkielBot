using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielCore.Messaging;
using ZaphkielShared.Commands;
using ZaphkielShared.Messaging;

namespace ZaphkielTests.Core;

public class QueuedCommandDispatcherTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IQueueMessageSerializer _serializer = Substitute.For<IQueueMessageSerializer>();

    private QueuedCommandDispatcher Dispatcher => new(_mediator, _serializer, NullLogger<QueuedCommandDispatcher>.Instance);

    [Fact]
    public async Task Deserializes_the_message_and_sends_the_command_through_the_mediator()
    {
        var command = new SendDiscordMessageCommand("c", "hello");
        _serializer.Deserialize("raw").Returns(command);

        await Dispatcher.DispatchAsync("raw", CancellationToken.None);

        await _mediator.Received(1).Send<Unit>(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Undeserializable_messages_fail_so_the_runtime_can_poison_them()
    {
        _serializer.Deserialize("garbage").Returns(_ => throw new System.Text.Json.JsonException("nope"));

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => Dispatcher.DispatchAsync("garbage", CancellationToken.None));
        await _mediator.DidNotReceiveWithAnyArgs().Send<Unit>(default!, default);
    }
}

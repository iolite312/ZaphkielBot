using Microsoft.Extensions.Logging;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielShared.Messaging;

namespace ZaphkielCore.Messaging;

public sealed class QueuedCommandDispatcher(
    IMediator mediator,
    IQueueMessageSerializer serializer,
    ILogger<QueuedCommandDispatcher> logger) : IQueuedCommandDispatcher
{
    public async Task DispatchAsync(string message, CancellationToken cancellationToken)
    {
        var command = serializer.Deserialize(message);
        logger.LogInformation("Dispatching queued command {CommandType}", command.GetType().Name);
        await mediator.Send<Unit>(command, cancellationToken);
    }
}

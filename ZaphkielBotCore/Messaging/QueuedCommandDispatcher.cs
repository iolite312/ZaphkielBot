using Microsoft.Extensions.Logging;
using ZaphkielBotCore.Cqrs;

namespace ZaphkielBotCore.Messaging;

/// <summary>Turns a raw queue message back into a command and runs it through the mediator.</summary>
public sealed class QueuedCommandDispatcher(IMediator mediator, ILogger<QueuedCommandDispatcher> logger)
{
    public async Task DispatchAsync(string message, CancellationToken cancellationToken)
    {
        var command = CommandEnvelope.Deserialize(message);
        logger.LogInformation("Dispatching queued command {CommandType}", command.GetType().Name);
        await mediator.Send(command, cancellationToken);
    }
}

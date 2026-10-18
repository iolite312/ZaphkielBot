using ZaphkielBot.Core.Cqrs;

namespace ZaphkielBot.Core.Messaging;

/// <summary>A command that is sent through an Azure Storage Queue instead of being executed in-process.</summary>
public interface IQueuedCommand : ICommand<Unit>;

public interface ICommandQueue
{
    Task EnqueueAsync(string queueName, IQueuedCommand command, CancellationToken cancellationToken);
}

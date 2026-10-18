using ZaphkielShared.Messaging;

namespace ZaphkielCore.Abstractions;

public interface ICommandQueue
{
    Task EnqueueAsync(string queueName, IQueuedCommand command, CancellationToken cancellationToken);
}

public interface IQueueMessageSerializer
{
    string Serialize(IQueuedCommand command);

    IQueuedCommand Deserialize(string message);
}

public interface IQueuedCommandDispatcher
{
    Task DispatchAsync(string message, CancellationToken cancellationToken);
}

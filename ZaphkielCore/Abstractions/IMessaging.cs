using ZaphkielShared.Messaging;

namespace ZaphkielCore.Abstractions;

public interface ICommandQueue
{
    Task EnqueueAsync(string queueName, IQueuedCommand command, CancellationToken cancellationToken);
}

/// <summary>Converts queued commands to and from the text stored in a queue message.</summary>
public interface IQueueMessageSerializer
{
    string Serialize(IQueuedCommand command);

    /// <exception cref="System.Text.Json.JsonException">The message is malformed or names an unknown command.</exception>
    IQueuedCommand Deserialize(string message);
}

/// <summary>Turns a raw queue message back into a command and runs it.</summary>
public interface IQueuedCommandDispatcher
{
    Task DispatchAsync(string message, CancellationToken cancellationToken);
}

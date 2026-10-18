using ZaphkielBotCore.Messaging;

namespace ZaphkielBot.Tests.Fakes;

public sealed class RecordingCommandQueue : ICommandQueue
{
    public List<(string Queue, IQueuedCommand Command)> Sent { get; } = [];

    public Task EnqueueAsync(string queueName, IQueuedCommand command, CancellationToken cancellationToken)
    {
        Sent.Add((queueName, command));
        return Task.CompletedTask;
    }
}

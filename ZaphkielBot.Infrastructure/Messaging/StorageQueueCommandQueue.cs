using Azure.Storage.Queues;
using ZaphkielBot.Core.Messaging;

namespace ZaphkielBot.Infrastructure.Messaging;

public sealed class StorageQueueCommandQueue(QueueServiceClient queues) : ICommandQueue
{
    public Task EnqueueAsync(string queueName, IQueuedCommand command, CancellationToken cancellationToken) =>
        queues.GetQueueClient(queueName).SendMessageAsync(CommandEnvelope.Serialize(command), cancellationToken);
}

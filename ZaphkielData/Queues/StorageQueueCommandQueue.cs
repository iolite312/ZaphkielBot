using Azure.Storage.Queues;
using ZaphkielCore.Abstractions;
using ZaphkielShared.Messaging;

namespace ZaphkielData.Queues;

public sealed class StorageQueueCommandQueue(QueueServiceClient queues, IQueueMessageSerializer serializer) : ICommandQueue
{
    public Task EnqueueAsync(string queueName, IQueuedCommand command, CancellationToken cancellationToken) =>
        queues.GetQueueClient(queueName).SendMessageAsync(serializer.Serialize(command), cancellationToken);
}

using ZaphkielCore.Abstractions;
using ZaphkielShared.Commands;
using ZaphkielShared.Messaging;

namespace ZaphkielData.Queues;

public class QueueDiscordNotifier(ICommandQueue queue) : IDiscordNotifier
{
    public Task PostMessageAsync(string channelId, string content, CancellationToken cancellationToken) =>
        queue.EnqueueAsync(QueueNames.DiscordNotifications, new SendDiscordMessageCommand(channelId, content), cancellationToken);
}

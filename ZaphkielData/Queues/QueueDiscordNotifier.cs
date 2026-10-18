using ZaphkielCore.Abstractions;
using ZaphkielShared.Commands;
using ZaphkielShared.Messaging;
using ZaphkielShared.Models;

namespace ZaphkielData.Queues;

public class QueueDiscordNotifier(ICommandQueue queue) : IDiscordNotifier
{
    public Task PostEmbedAsync(string channelId, EmbedMessage embed, CancellationToken cancellationToken) =>
        queue.EnqueueAsync(QueueNames.DiscordNotifications, new SendDiscordMessageCommand(channelId, embed), cancellationToken);
}

using ZaphkielCore.Abstractions;
using ZaphkielShared.Commands;
using ZaphkielShared.Messaging;

namespace ZaphkielData.Queues;

/// <summary>
/// Notifies Discord by queueing a <see cref="SendDiscordMessageCommand"/>; the bot's queue function does the actual post.
/// A Discord outage therefore never fails (or blocks) the incident processing.
/// </summary>
public sealed class QueueDiscordNotifier(ICommandQueue queue) : IDiscordNotifier
{
    public Task PostMessageAsync(string channelId, string content, CancellationToken cancellationToken) =>
        queue.EnqueueAsync(QueueNames.DiscordNotifications, new SendDiscordMessageCommand(channelId, content), cancellationToken);
}

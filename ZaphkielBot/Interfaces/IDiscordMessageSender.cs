using ZaphkielShared.Models;

namespace ZaphkielBot.Interfaces;

public interface IDiscordMessageSender
{
    Task SendAsync(string channelId, EmbedMessage embed, CancellationToken cancellationToken);
}

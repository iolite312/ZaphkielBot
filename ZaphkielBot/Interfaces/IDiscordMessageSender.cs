namespace ZaphkielBot.Discord;

public interface IDiscordMessageSender
{
    Task SendAsync(string channelId, string content, CancellationToken cancellationToken);
}

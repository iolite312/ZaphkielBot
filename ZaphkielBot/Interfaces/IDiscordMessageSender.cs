namespace ZaphkielBot.Interfaces;

public interface IDiscordMessageSender
{
    Task SendAsync(string channelId, string content, CancellationToken cancellationToken);
}

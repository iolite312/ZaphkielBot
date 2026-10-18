using global::Discord;

namespace ZaphkielBot.Discord;

/// <summary>Posts messages into a channel through Discord.Net.</summary>
public sealed class DiscordMessageSender(IDiscordRestClientProvider provider) : IDiscordMessageSender
{
    public async Task SendAsync(string channelId, string content, CancellationToken cancellationToken)
    {
        if (!ulong.TryParse(channelId, out var id))
            throw new ArgumentException($"'{channelId}' is not a valid Discord channel id.", nameof(channelId));

        await provider.EnsureLoggedInAsync(cancellationToken);
        var options = new RequestOptions { CancelToken = cancellationToken };

        var channel = await provider.Client.GetChannelAsync(id, options) as IMessageChannel
                      ?? throw new InvalidOperationException($"Channel {channelId} was not found or cannot receive messages.");

        // AllowedMentions.None: relayed text can never ping @everyone, roles or users.
        await channel.SendMessageAsync(content, allowedMentions: AllowedMentions.None, options: options);
    }
}

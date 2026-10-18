using Discord.Rest;

namespace ZaphkielBot.Discord;

/// <summary>Gives the rest of the bot access to the Discord.Net REST client, logging it in on demand.</summary>
public interface IDiscordRestClientProvider
{
    /// <summary>The client. Enough for parsing/verifying interactions; call <see cref="EnsureLoggedInAsync"/> before REST calls.</summary>
    DiscordRestClient Client { get; }

    Task EnsureLoggedInAsync(CancellationToken cancellationToken);
}

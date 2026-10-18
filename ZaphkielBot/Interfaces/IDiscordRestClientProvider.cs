using Discord.Rest;

namespace ZaphkielBot.Discord;

public interface IDiscordRestClientProvider
{
    DiscordRestClient Client { get; }

    Task EnsureLoggedInAsync(CancellationToken cancellationToken);
}

using Discord.Rest;

namespace ZaphkielBot.Interfaces;

public interface IDiscordRestClientProvider
{
    DiscordRestClient Client { get; }

    Task EnsureLoggedInAsync(CancellationToken cancellationToken);
}

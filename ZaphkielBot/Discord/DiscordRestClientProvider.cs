using Discord;
using Discord.Rest;
using Microsoft.Extensions.Options;
using ZaphkielBot.Interfaces;
using ZaphkielShared.Options;

namespace ZaphkielBot.Discord;

public class DiscordRestClientProvider(IOptions<DiscordOptions> options) : IDiscordRestClientProvider, IDisposable
{
    private readonly SemaphoreSlim _login = new(1, 1);

    public DiscordRestClient Client { get; } = new();

    public async Task EnsureLoggedInAsync(CancellationToken cancellationToken)
    {
        if (Client.LoginState == LoginState.LoggedIn) return;

        await _login.WaitAsync(cancellationToken);
        try
        {
            if (Client.LoginState != LoginState.LoggedIn)
                await Client.LoginAsync(TokenType.Bot, options.Value.BotToken);
        }
        finally
        {
            _login.Release();
        }
    }

    public void Dispose()
    {
        _login.Dispose();
        Client.Dispose();
    }
}

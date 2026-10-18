using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZaphkielShared.Options;

namespace ZaphkielBot.Discord;

public class SlashCommandRegistrar(
    IDiscordRestClientProvider provider,
    InteractionService interactions,
    IOptions<DiscordOptions> options,
    ILogger<SlashCommandRegistrar> logger) : ISlashCommandRegistrar
{
    public async Task RegisterAsync(CancellationToken cancellationToken)
    {
        await provider.EnsureLoggedInAsync(cancellationToken);

        if (ulong.TryParse(options.Value.GuildId, out var guildId))
        {
            var commands = await interactions.RegisterCommandsToGuildAsync(guildId);
            logger.LogInformation("Registered {Count} slash command(s) to guild {GuildId}", commands.Count, guildId);
        }
        else
        {
            var commands = await interactions.RegisterCommandsGloballyAsync();
            logger.LogInformation("Registered {Count} slash command(s) globally", commands.Count);
        }
    }
}

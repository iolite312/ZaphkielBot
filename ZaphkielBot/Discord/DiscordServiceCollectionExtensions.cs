using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using ZaphkielBot.Interfaces;

namespace ZaphkielBot.Discord;

public static class DiscordServiceCollectionExtensions
{
    public static IServiceCollection AddZaphkielDiscord(this IServiceCollection services)
    {
        services.AddSingleton<IDiscordRestClientProvider, DiscordRestClientProvider>();

        services.AddSingleton(sp =>
        {
            // Sync: the module has answered (or failed) by the time ExecuteCommandAsync returns.
            var service = new InteractionService(
                sp.GetRequiredService<IDiscordRestClientProvider>().Client,
                new InteractionServiceConfig { DefaultRunMode = RunMode.Sync });

            service.AddModulesAsync(typeof(IncidentModule).Assembly, sp).GetAwaiter().GetResult();
            return service;
        });

        services.AddScoped<IIncidentCommandService, IncidentCommandService>();
        services.AddScoped<IDiscordInteractionHandler, DiscordInteractionHandler>();
        services.AddSingleton<IEmbedMapper, DiscordEmbedMapper>();
        services.AddSingleton<IDiscordMessageSender, DiscordMessageSender>();

        services.AddSingleton<ISlashCommandRegistrar, SlashCommandRegistrar>();
        services.AddHostedService<SlashCommandRegistrationService>();

        return services;
    }
}

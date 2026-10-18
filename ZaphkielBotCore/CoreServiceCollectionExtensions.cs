using Microsoft.Extensions.DependencyInjection;
using ZaphkielBotCore.Cqrs;
using ZaphkielBotCore.Discord;
using ZaphkielBotCore.GitHub;
using ZaphkielBotCore.Messaging;

namespace ZaphkielBotCore;

public static class CoreServiceCollectionExtensions
{
    /// <summary>Registers the business tier. The data tier (stores, queue, HTTP clients) comes from the Infrastructure project.</summary>
    public static IServiceCollection AddZaphkielCore(this IServiceCollection services)
    {
        services.AddCqrs(typeof(CoreServiceCollectionExtensions).Assembly);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<QueuedCommandDispatcher>();
        services.AddScoped<IDiscordInteractionService, DiscordInteractionService>();
        services.AddScoped<IGitHubWebhookService, GitHubWebhookService>();
        return services;
    }
}

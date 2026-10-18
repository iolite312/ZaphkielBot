using Microsoft.Extensions.DependencyInjection;
using ZaphkielBot.Core.Cqrs;
using ZaphkielBot.Core.Discord;
using ZaphkielBot.Core.GitHub;
using ZaphkielBot.Core.Messaging;

namespace ZaphkielBot.Core;

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

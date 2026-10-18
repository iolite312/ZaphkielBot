using System.Net.Http.Headers;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZaphkielBotCore.Abstractions;
using ZaphkielBotCore.Discord;
using ZaphkielBotCore.GitHub;
using ZaphkielBotCore.Messaging;
using ZaphkielBotInfrastructure.Http;
using ZaphkielBotInfrastructure.Messaging;
using ZaphkielBotInfrastructure.Security;
using ZaphkielBotInfrastructure.Storage;

namespace ZaphkielBotInfrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>The Functions host already requires this setting for queue triggers, so it doubles as the datastore connection.</summary>
    public const string StorageConnectionSetting = "AzureWebJobsStorage";

    public static IServiceCollection AddZaphkielInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Fail at startup, with a clear message, instead of on the first incident.
        services.AddOptions<DiscordOptions>().Bind(configuration.GetSection(DiscordOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<GitHubOptions>().Bind(configuration.GetSection(GitHubOptions.Section)).ValidateDataAnnotations().ValidateOnStart();

        var storageConnection = configuration[StorageConnectionSetting];
        if (string.IsNullOrWhiteSpace(storageConnection))
            throw new InvalidOperationException($"The '{StorageConnectionSetting}' setting is required.");

        services.AddSingleton(new TableServiceClient(storageConnection));
        // The Functions queue trigger expects base64 encoded messages by default.
        services.AddSingleton(new QueueServiceClient(storageConnection, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));
        services.AddHostedService<StorageInitializer>();

        services.AddSingleton<TableIncidentStore>();
        services.AddSingleton<IIncidentReader>(sp => sp.GetRequiredService<TableIncidentStore>());
        services.AddSingleton<IIncidentWriter>(sp => sp.GetRequiredService<TableIncidentStore>());
        services.AddSingleton<ICommandQueue, StorageQueueCommandQueue>();

        services.AddSingleton<IDiscordSignatureVerifier, DiscordSignatureVerifier>();
        services.AddSingleton<IGitHubSignatureVerifier, GitHubSignatureVerifier>();

        services.AddHttpClient<IGitHubIssueClient, GitHubRestClient>((sp, http) =>
        {
            http.BaseAddress = new Uri("https://api.github.com/");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sp.GetRequiredService<IOptions<GitHubOptions>>().Value.Token);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ZaphkielBot", "1.0"));
            http.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddHttpClient<IDiscordNotifier, DiscordRestNotifier>((sp, http) =>
        {
            http.BaseAddress = new Uri("https://discord.com/api/v10/");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", sp.GetRequiredService<IOptions<DiscordOptions>>().Value.BotToken);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordBot (ZaphkielBot, 1.0)");
            http.Timeout = TimeSpan.FromSeconds(20);
        });

        return services;
    }
}

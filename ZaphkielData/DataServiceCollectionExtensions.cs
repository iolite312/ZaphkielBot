using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZaphkielCore.Abstractions;
using ZaphkielData.Mapping;
using ZaphkielData.Queues;
using ZaphkielData.Repositories;
using ZaphkielData.Storage;

namespace ZaphkielData;

public static class DataServiceCollectionExtensions
{
    /// <summary>The Functions host already requires this setting for queue triggers, so it doubles as the datastore connection.</summary>
    public const string StorageConnectionSetting = "AzureWebJobsStorage";

    public static IServiceCollection AddZaphkielData(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration[StorageConnectionSetting];
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException($"The '{StorageConnectionSetting}' setting is required.");

        services.AddSingleton(new TableServiceClient(connection));
        // The Functions queue trigger expects base64 encoded messages by default.
        services.AddSingleton(new QueueServiceClient(connection, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));
        services.AddHostedService<StorageInitializer>();

        services.AddSingleton<IIncidentMapper, IncidentMapper>();
        services.AddSingleton<IIncidentReadRepository, TableIncidentReadRepository>();
        services.AddSingleton<IIncidentWriteRepository, TableIncidentWriteRepository>();

        services.AddSingleton<IQueueMessageSerializer, JsonQueueMessageSerializer>();
        services.AddSingleton<ICommandQueue, StorageQueueCommandQueue>();
        services.AddSingleton<IDiscordNotifier, QueueDiscordNotifier>();

        return services;
    }
}

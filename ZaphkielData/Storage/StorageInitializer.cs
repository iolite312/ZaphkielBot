using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZaphkielData.Entities;
using ZaphkielShared.Messaging;

namespace ZaphkielData.Storage;

public class StorageInitializer(
    TableServiceClient tables,
    QueueServiceClient queues,
    ILogger<StorageInitializer> logger) : IHostedService
{
    private const int MaxAttempts = 30;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await tables.CreateTableIfNotExistsAsync(TableNames.Incidents, cancellationToken);
                await tables.CreateTableIfNotExistsAsync(TableNames.IncidentLookup, cancellationToken);
                foreach (var queue in QueueNames.All)
                    await queues.GetQueueClient(queue).CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                logger.LogInformation("Storage tables and queues are ready");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && ex is not OperationCanceledException)
            {
                logger.LogWarning("Storage not ready yet (attempt {Attempt}/{Max}): {Message}", attempt, MaxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

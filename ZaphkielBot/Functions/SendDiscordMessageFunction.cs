using Microsoft.Azure.Functions.Worker;
using ZaphkielCore.Abstractions;
using ZaphkielShared.Messaging;

namespace ZaphkielBot.Functions;

public class SendDiscordMessageFunction(IQueuedCommandDispatcher dispatcher)
{
    [Function("SendDiscordMessage")]
    public Task Run(
        [QueueTrigger(QueueNames.DiscordNotifications, Connection = "AzureWebJobsStorage")] string message,
        CancellationToken cancellationToken) =>
        dispatcher.DispatchAsync(message, cancellationToken);
}

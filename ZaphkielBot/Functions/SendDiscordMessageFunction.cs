using Microsoft.Azure.Functions.Worker;
using ZaphkielCore.Abstractions;
using ZaphkielShared.Messaging;

namespace ZaphkielBot.Functions;

/// <summary>
/// Posts the messages the processor asked for (issue links, GitHub comments, resolutions) into Discord.
/// Failures are retried by the runtime (5 attempts) and then parked in "discord-notifications-poison".
/// </summary>
public class SendDiscordMessageFunction(IQueuedCommandDispatcher dispatcher)
{
    [Function("SendDiscordMessage")]
    public Task Run(
        [QueueTrigger(QueueNames.DiscordNotifications, Connection = "AzureWebJobsStorage")] string message,
        CancellationToken cancellationToken) =>
        dispatcher.DispatchAsync(message, cancellationToken);
}

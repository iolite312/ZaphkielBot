using Microsoft.Azure.Functions.Worker;
using ZaphkielBotCore.Messaging;

namespace ZaphkielBot.Functions;

/// <summary>
/// Command side. Each queue has its own trigger so they scale and poison independently.
/// An exception makes the runtime retry the message (5 attempts) and then park it in "&lt;queue&gt;-poison".
/// </summary>
public class QueueProcessorFunctions(QueuedCommandDispatcher dispatcher)
{
    private const string Connection = "AzureWebJobsStorage";

    [Function("ProcessIncidentCommand")]
    public Task ProcessIncidentCommand(
        [QueueTrigger(QueueNames.IncidentCommands, Connection = Connection)] string message,
        CancellationToken cancellationToken) =>
        dispatcher.DispatchAsync(message, cancellationToken);

    [Function("ProcessGitHubEvent")]
    public Task ProcessGitHubEvent(
        [QueueTrigger(QueueNames.GitHubEvents, Connection = Connection)] string message,
        CancellationToken cancellationToken) =>
        dispatcher.DispatchAsync(message, cancellationToken);
}

using Microsoft.Azure.Functions.Worker;
using ZaphkielCore.Abstractions;
using ZaphkielShared.Messaging;

namespace ZaphkielProcessor.Functions;

public class QueueProcessorFunctions(IQueuedCommandDispatcher dispatcher)
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

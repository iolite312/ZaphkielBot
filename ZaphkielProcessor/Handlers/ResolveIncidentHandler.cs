using Microsoft.Extensions.Logging;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielCore.Formatting;
using ZaphkielShared.Commands;
using ZaphkielShared.Domain;
using ZaphkielShared.Messaging;

namespace ZaphkielProcessor.Handlers;

public sealed class ResolveIncidentHandler(
    IIncidentReadRepository reader,
    IIncidentWriteRepository writer,
    IGitHubIssueClient gitHub,
    IDiscordNotifier discord,
    ILogger<ResolveIncidentHandler> logger) : ICommandHandler<ResolveIncidentCommand, Unit>
{
    public async Task<Unit> Handle(ResolveIncidentCommand command, CancellationToken cancellationToken)
    {
        var incident = command switch
        {
            { IncidentId: { } id } => await reader.GetByIdAsync(id, cancellationToken),
            { GitHubIssueNumber: { } number } => await reader.GetByGitHubIssueAsync(number, cancellationToken),
            _ => null
        };

        if (incident is null)
        {
            // Normal for webhooks about issues that were not opened by this bot.
            logger.LogInformation("No incident found for resolve command ({IncidentId}, issue {Issue})", command.IncidentId, command.GitHubIssueNumber);
            return Unit.Value;
        }

        if (!incident.Resolve(command.ResolvedAt))
        {
            logger.LogInformation("Incident {IncidentId} is already resolved", incident.Id);
            return Unit.Value;
        }

        await writer.UpdateAsync(incident, cancellationToken);

        if (command.Source == ResolutionSource.Discord && incident.GitHubIssueNumber is { } issueNumber)
            await gitHub.CloseIssueAsync(issueNumber, cancellationToken);

        var where = command.Source == ResolutionSource.GitHub ? "closed on GitHub" : "resolved";
        try
        {
            await discord.PostMessageAsync(
                incident.ChannelId,
                $"✅ **{IncidentId.Display(incident.Id)}** {where} by **{command.ResolvedBy}**. Time to resolve: {MessageFormat.Duration(incident.TimeToResolve!.Value)}.",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not queue the Discord announcement for {IncidentId}", incident.Id);
        }

        return Unit.Value;
    }
}

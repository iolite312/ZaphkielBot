using Microsoft.Extensions.Logging;
using ZaphkielBotCore.Abstractions;
using ZaphkielBotCore.Cqrs;
using ZaphkielBotCore.Domain;
using ZaphkielBotCore.Messaging;

namespace ZaphkielBotCore.Commands;

public sealed record CreateIncidentCommand(
    string IncidentId,
    string Title,
    string? Description,
    Severity Severity,
    string ReporterId,
    string ReporterName,
    string ChannelId,
    DateTimeOffset ReportedAt) : IQueuedCommand;

/// <summary>
/// Logs the incident, opens the tracking issue on GitHub and links the two.
/// Queue delivery is at-least-once, so every step tolerates being run again.
/// </summary>
public sealed class CreateIncidentHandler(
    IIncidentReader reader,
    IIncidentWriter writer,
    IGitHubIssueClient gitHub,
    IDiscordNotifier discord,
    ILogger<CreateIncidentHandler> logger) : ICommandHandler<CreateIncidentCommand, Unit>
{
    public async Task<Unit> Handle(CreateIncidentCommand command, CancellationToken cancellationToken)
    {
        var incident = await reader.GetByIdAsync(command.IncidentId, cancellationToken)
                       ?? Incident.Report(
                           command.IncidentId, command.Title, command.Description, command.Severity,
                           command.ReporterId, command.ReporterName, command.ChannelId, command.ReportedAt);

        if (incident.GitHubIssueNumber is not null)
        {
            logger.LogInformation("Incident {IncidentId} already has issue #{Issue}, skipping", incident.Id, incident.GitHubIssueNumber);
            return Unit.Value;
        }

        await writer.AddAsync(incident, cancellationToken);

        var issue = await gitHub.CreateIssueAsync(
            new NewGitHubIssue(
                $"[{IncidentId.Display(incident.Id)}] {incident.Title}",
                BuildIssueBody(incident),
                ["incident", incident.Severity.GitHubLabel()]),
            cancellationToken);

        incident.LinkGitHubIssue(issue.Number, issue.Url);
        await writer.UpdateAsync(incident, cancellationToken);

        logger.LogInformation("Incident {IncidentId} logged and linked to issue #{Issue}", incident.Id, issue.Number);

        try
        {
            await discord.PostMessageAsync(
                incident.ChannelId,
                $"🎫 **{IncidentId.Display(incident.Id)}** is now tracked as GitHub issue #{issue.Number}: {issue.Url}",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The incident is safely stored and linked; failing the command here would only re-run a finished job.
            logger.LogWarning(ex, "Could not announce the GitHub issue for {IncidentId} in Discord", incident.Id);
        }

        return Unit.Value;
    }

    private static string BuildIssueBody(Incident incident) =>
        $"""
         **Incident `{IncidentId.Display(incident.Id)}`** reported via Discord by {NeutralizeMentions(incident.ReporterName)}

         | | |
         |---|---|
         | Severity | {incident.Severity.Label()} |
         | Reported | {incident.CreatedAt:yyyy-MM-dd HH:mm} UTC |

         ### Description
         {(incident.Description.Length == 0 ? "_No description provided._" : NeutralizeMentions(incident.Description))}

         ---
         _Comments on this issue are relayed to Discord. Closing the issue resolves the incident._
         """;

    /// <summary>Stops user supplied text from pinging GitHub users or teams.</summary>
    private static string NeutralizeMentions(string text) => text.Replace("@", "@​");
}

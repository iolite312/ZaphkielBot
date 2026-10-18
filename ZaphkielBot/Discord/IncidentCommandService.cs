using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZaphkielBot.Interfaces;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielCore.Formatting;
using ZaphkielShared.Commands;
using ZaphkielShared.Models;
using ZaphkielShared.Messaging;
using ZaphkielShared.Models;
using ZaphkielShared.Options;
using ZaphkielShared.Queries;

namespace ZaphkielBot.Discord;

public class IncidentCommandService(
    IMediator mediator,
    ICommandQueue queue,
    IOptions<DiscordOptions> options,
    TimeProvider clock,
    ILogger<IncidentCommandService> logger) : IIncidentCommandService
{
    public Task<BotReply> ReportAsync(
        InvocationContext context, string? title, string? severity, string? description, CancellationToken cancellationToken) =>
        GuardAsync(context, nameof(ReportAsync), async () =>
        {
            // Announcements are posted back to this channel, so an incident without one could never be followed up.
            if (string.IsNullOrEmpty(context.ChannelId))
                return BotReply.Refused("I couldn't tell which channel this came from, so I can't report an incident.");

            if (!SeverityExtensions.TryParseSeverity(severity, out var parsedSeverity))
                return BotReply.Refused("Choose a severity: critical, high, medium or low.");

            if (Incident.ValidateReport(title, description) is { } error)
                return BotReply.Refused(error);

            var id = IncidentId.New();
            await queue.EnqueueAsync(
                QueueNames.IncidentCommands,
                new CreateIncidentCommand(
                    id, title!.Trim(), description?.Trim(), parsedSeverity,
                    context.UserId, context.UserName, context.ChannelId, clock.GetUtcNow()),
                cancellationToken);

            return BotReply.Public(new EmbedMessage(
                $"🚨 {IncidentId.Display(id)} reported",
                title.Trim(),
                Style: EmbedStyles.For(parsedSeverity),
                Fields:
                [
                    new EmbedField("Severity", parsedSeverity.Label()),
                    new EmbedField("Reported by", $"<@{context.UserId}>")
                ],
                Footer: "Opening a GitHub issue…",
                Timestamp: clock.GetUtcNow()));
        });

    public Task<BotReply> ResolveAsync(InvocationContext context, string? id, CancellationToken cancellationToken) =>
        GuardAsync(context, nameof(ResolveAsync), async () =>
        {
            if (IncidentId.Normalize(id) is not { } incidentId)
                return BotReply.Refused(InvalidIdMessage);

            var incident = await mediator.Query(new GetIncidentQuery(incidentId), cancellationToken);
            if (incident is null)
                return BotReply.Refused($"I can't find {IncidentId.Display(incidentId)}.");
            if (incident.Status == IncidentStatus.Resolved)
                return BotReply.Refused($"{IncidentId.Display(incidentId)} is already resolved.");

            await queue.EnqueueAsync(
                QueueNames.IncidentCommands,
                new ResolveIncidentCommand(incidentId, null, context.UserName, ResolutionSource.Discord, clock.GetUtcNow()),
                cancellationToken);

            return BotReply.Public(new EmbedMessage($"⏳ Resolving {IncidentId.Display(incidentId)}…", incident.Title));
        });

    public Task<BotReply> StatusAsync(InvocationContext context, string? id, CancellationToken cancellationToken) =>
        GuardAsync(context, nameof(StatusAsync), async () =>
        {
            if (IncidentId.Normalize(id) is not { } incidentId)
                return BotReply.Refused(InvalidIdMessage);

            var incident = await mediator.Query(new GetIncidentQuery(incidentId), cancellationToken);
            if (incident is null)
                return BotReply.Refused($"I can't find {IncidentId.Display(incidentId)}.");

            var resolved = incident.Status == IncidentStatus.Resolved;

            return BotReply.Private(new EmbedMessage(
                $"{IncidentId.Display(incident.Id)}: {incident.Title}",
                incident.Description.Length == 0 ? null : incident.Description,
                incident.GitHubIssueUrl,
                resolved ? EmbedStyle.Success : EmbedStyles.For(incident.Severity),
                [
                    new EmbedField("Severity", incident.Severity.Label()),
                    new EmbedField("Status", resolved ? $"Resolved in {MessageFormat.Duration(incident.TimeToResolveMinutes)}" : "Open"),
                    new EmbedField("Reported by", incident.ReportedBy),
                    new EmbedField("GitHub issue", incident.GitHubIssueUrl is null ? "Pending" : $"[#{incident.GitHubIssueNumber}]({incident.GitHubIssueUrl})")
                ],
                Timestamp: incident.CreatedAt));
        });

    public Task<BotReply> StatsAsync(InvocationContext context, CancellationToken cancellationToken) =>
        GuardAsync(context, nameof(StatsAsync), async () =>
        {
            var now = clock.GetUtcNow();
            var report = await mediator.Query(new GetAnalyticsQuery(now.AddDays(-30), now), cancellationToken);

            var fields = report.BySeverity
                .Where(s => s.Total > 0)
                .Select(s => new EmbedField(
                    s.Severity.Label(),
                    $"{s.Total} total · {s.Open} open\nMTTR {MessageFormat.Duration(s.MeanTimeToResolveMinutes)}"))
                .ToList();

            return BotReply.Private(new EmbedMessage(
                "📊 Incidents · last 30 days",
                $"{report.Total} incidents · {report.Open} open · {report.Resolved} resolved\nMTTR {MessageFormat.Duration(report.MeanTimeToResolveMinutes)}",
                Fields: fields));
        });

    private const string InvalidIdMessage = "That doesn't look like an incident id (e.g. INC-a1b2c3d4).";

    private async Task<BotReply> GuardAsync(InvocationContext context, string operation, Func<Task<BotReply>> action)
    {
        var designatedChannel = options.Value.IncidentChannelId;
        if (!string.IsNullOrEmpty(designatedChannel) && context.ChannelId != designatedChannel)
            return BotReply.Refused($"Incidents can only be managed in <#{designatedChannel}>.");

        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to handle {Operation}", operation);
            return BotReply.Failed();
        }
    }
}

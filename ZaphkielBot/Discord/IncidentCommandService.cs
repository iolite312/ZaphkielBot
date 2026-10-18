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
                return BotReply.Private("I couldn't tell which channel this came from, so I can't report an incident.");

            if (!SeverityExtensions.TryParseSeverity(severity, out var parsedSeverity))
                return BotReply.Private("Choose a severity: critical, high, medium or low.");

            if (Incident.ValidateReport(title, description) is { } error)
                return BotReply.Private(error);

            var id = IncidentId.New();
            await queue.EnqueueAsync(
                QueueNames.IncidentCommands,
                new CreateIncidentCommand(
                    id, title!.Trim(), description?.Trim(), parsedSeverity,
                    context.UserId, context.UserName, context.ChannelId, clock.GetUtcNow()),
                cancellationToken);

            return BotReply.Public(
                $"🚨 **{IncidentId.Display(id)}** reported by <@{context.UserId}> — **{parsedSeverity.Label()}**: {title.Trim()}\n" +
                "I'm opening a GitHub issue for it now.");
        });

    public Task<BotReply> ResolveAsync(InvocationContext context, string? id, CancellationToken cancellationToken) =>
        GuardAsync(context, nameof(ResolveAsync), async () =>
        {
            if (IncidentId.Normalize(id) is not { } incidentId)
                return BotReply.Private(InvalidIdMessage);

            var incident = await mediator.Query(new GetIncidentQuery(incidentId), cancellationToken);
            if (incident is null)
                return BotReply.Private($"I can't find {IncidentId.Display(incidentId)}.");
            if (incident.Status == IncidentStatus.Resolved)
                return BotReply.Private($"{IncidentId.Display(incidentId)} is already resolved.");

            await queue.EnqueueAsync(
                QueueNames.IncidentCommands,
                new ResolveIncidentCommand(incidentId, null, context.UserName, ResolutionSource.Discord, clock.GetUtcNow()),
                cancellationToken);

            return BotReply.Public($"Resolving **{IncidentId.Display(incidentId)}**…");
        });

    public Task<BotReply> StatusAsync(InvocationContext context, string? id, CancellationToken cancellationToken) =>
        GuardAsync(context, nameof(StatusAsync), async () =>
        {
            if (IncidentId.Normalize(id) is not { } incidentId)
                return BotReply.Private(InvalidIdMessage);

            var incident = await mediator.Query(new GetIncidentQuery(incidentId), cancellationToken);
            if (incident is null)
                return BotReply.Private($"I can't find {IncidentId.Display(incidentId)}.");

            var issue = incident.GitHubIssueUrl ?? "issue pending";
            var state = incident.Status == IncidentStatus.Resolved
                ? $"resolved in {MessageFormat.Duration(incident.TimeToResolveMinutes)}"
                : "open";

            return BotReply.Private(
                $"**{IncidentId.Display(incident.Id)}** — {incident.Severity.Label()} — {state}\n" +
                $"{incident.Title}\nReported by {incident.ReportedBy} • {issue}");
        });

    public Task<BotReply> StatsAsync(InvocationContext context, CancellationToken cancellationToken) =>
        GuardAsync(context, nameof(StatsAsync), async () =>
        {
            var now = clock.GetUtcNow();
            var report = await mediator.Query(new GetAnalyticsQuery(now.AddDays(-30), now), cancellationToken);

            var summary = $"📊 **Last 30 days**: {report.Total} incidents, {report.Open} open, {report.Resolved} resolved. " +
                          $"MTTR {MessageFormat.Duration(report.MeanTimeToResolveMinutes)}";
            var breakdown = report.BySeverity
                .Where(s => s.Total > 0)
                .Select(s => $"• {s.Severity.Label()}: {s.Total} ({s.Open} open) — MTTR {MessageFormat.Duration(s.MeanTimeToResolveMinutes)}")
                .ToList();

            return BotReply.Private(breakdown.Count == 0 ? summary : $"{summary}\n{string.Join('\n', breakdown)}");
        });

    private const string InvalidIdMessage = "That doesn't look like an incident id (e.g. INC-a1b2c3d4).";

    private async Task<BotReply> GuardAsync(InvocationContext context, string operation, Func<Task<BotReply>> action)
    {
        var designatedChannel = options.Value.IncidentChannelId;
        if (!string.IsNullOrEmpty(designatedChannel) && context.ChannelId != designatedChannel)
            return BotReply.Private($"Incidents can only be managed in <#{designatedChannel}>.");

        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to handle {Operation}", operation);
            return BotReply.Private("⚠️ Something went wrong on my side. Please try again in a moment.");
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZaphkielBotCore.Abstractions;
using ZaphkielBotCore.Commands;
using ZaphkielBotCore.Cqrs;
using ZaphkielBotCore.Domain;
using ZaphkielBotCore.Messaging;
using ZaphkielBotCore.Queries;

namespace ZaphkielBotCore.Discord;

public enum InteractionOutcome
{
    Ok,
    Unauthorized,
    BadRequest
}

public sealed record InteractionResult(InteractionOutcome Outcome, InteractionResponse? Response = null);

public interface IDiscordInteractionService
{
    Task<InteractionResult> HandleAsync(string? signature, string? timestamp, byte[] body, CancellationToken cancellationToken);
}

/// <summary>
/// Business tier for the /incident slash command. Reads are answered directly through the mediator (query side);
/// writes are validated, then queued and acknowledged straight away so Discord's 3 second deadline is always met.
/// </summary>
public sealed class DiscordInteractionService(
    IDiscordSignatureVerifier signatureVerifier,
    IMediator mediator,
    ICommandQueue queue,
    IOptions<DiscordOptions> options,
    TimeProvider clock,
    ILogger<DiscordInteractionService> logger) : IDiscordInteractionService
{
    private const int InteractionPing = 1;
    private const int InteractionApplicationCommand = 2;
    private const string CommandName = "incident";

    public async Task<InteractionResult> HandleAsync(
        string? signature, string? timestamp, byte[] body, CancellationToken cancellationToken)
    {
        if (!signatureVerifier.Verify(timestamp, body, signature))
        {
            logger.LogWarning("Rejected a Discord interaction with an invalid signature");
            return new InteractionResult(InteractionOutcome.Unauthorized);
        }

        DiscordInteraction? interaction;
        try
        {
            interaction = JsonSerializer.Deserialize<DiscordInteraction>(body, DiscordJson.Options);
        }
        catch (JsonException)
        {
            interaction = null;
        }

        if (interaction is null)
            return new InteractionResult(InteractionOutcome.BadRequest);

        var response = interaction.Type switch
        {
            InteractionPing => InteractionResponse.Pong(),
            InteractionApplicationCommand => await HandleCommandAsync(interaction, cancellationToken),
            _ => InteractionResponse.Private("That interaction type is not supported.")
        };

        return new InteractionResult(InteractionOutcome.Ok, response);
    }

    private async Task<InteractionResponse> HandleCommandAsync(DiscordInteraction interaction, CancellationToken ct)
    {
        if (interaction.Data is not { Name: CommandName, Options: [{ } subcommand, ..] })
            return InteractionResponse.Private("Unknown command.");

        var designatedChannel = options.Value.IncidentChannelId;
        if (!string.IsNullOrEmpty(designatedChannel) && interaction.ChannelId != designatedChannel)
            return InteractionResponse.Private($"Incidents can only be managed in <#{designatedChannel}>.");

        try
        {
            return subcommand.Name switch
            {
                "report" => await ReportAsync(interaction, subcommand, ct),
                "resolve" => await ResolveAsync(interaction, subcommand, ct),
                "status" => await StatusAsync(subcommand, ct),
                "stats" => await StatsAsync(ct),
                _ => InteractionResponse.Private("Unknown subcommand.")
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to handle /incident {Subcommand}", subcommand.Name);
            return InteractionResponse.Private("⚠️ Something went wrong on my side. Please try again in a moment.");
        }
    }

    private async Task<InteractionResponse> ReportAsync(DiscordInteraction interaction, DiscordOption subcommand, CancellationToken ct)
    {
        var title = OptionString(subcommand, "title");
        var description = OptionString(subcommand, "description");

        if (!SeverityExtensions.TryParseSeverity(OptionString(subcommand, "severity"), out var severity))
            return InteractionResponse.Private("Choose a severity: critical, high, medium or low.");

        if (Incident.ValidateReport(title, description) is { } error)
            return InteractionResponse.Private(error);

        var reporter = interaction.Member?.User ?? interaction.User;
        if (reporter is null || interaction.ChannelId is null)
            return InteractionResponse.Private("I could not tell who reported this or where.");

        var reporterName = interaction.Member?.Nick ?? reporter.GlobalName ?? reporter.Username;
        var id = IncidentId.New();

        await queue.EnqueueAsync(
            QueueNames.IncidentCommands,
            new CreateIncidentCommand(
                id, title!.Trim(), description?.Trim(), severity,
                reporter.Id, reporterName, interaction.ChannelId, clock.GetUtcNow()),
            ct);

        return InteractionResponse.Message(
            $"🚨 **{IncidentId.Display(id)}** reported by <@{reporter.Id}> — **{severity.Label()}**: {title.Trim()}\n" +
            "I'm opening a GitHub issue for it now.");
    }

    private async Task<InteractionResponse> ResolveAsync(DiscordInteraction interaction, DiscordOption subcommand, CancellationToken ct)
    {
        var id = IncidentId.Normalize(OptionString(subcommand, "id"));
        if (id is null)
            return InteractionResponse.Private("That doesn't look like an incident id (e.g. INC-a1b2c3d4).");

        var incident = await mediator.Query(new GetIncidentQuery(id), ct);
        if (incident is null)
            return InteractionResponse.Private($"I can't find {IncidentId.Display(id)}.");
        if (incident.Status == IncidentStatus.Resolved)
            return InteractionResponse.Private($"{IncidentId.Display(id)} is already resolved.");

        var user = interaction.Member?.User ?? interaction.User;
        var resolvedBy = interaction.Member?.Nick ?? user?.GlobalName ?? user?.Username ?? "someone";

        await queue.EnqueueAsync(
            QueueNames.IncidentCommands,
            new ResolveIncidentCommand(id, null, resolvedBy, ResolutionSource.Discord, clock.GetUtcNow()),
            ct);

        return InteractionResponse.Message($"Resolving **{IncidentId.Display(id)}**…");
    }

    private async Task<InteractionResponse> StatusAsync(DiscordOption subcommand, CancellationToken ct)
    {
        var id = IncidentId.Normalize(OptionString(subcommand, "id"));
        if (id is null)
            return InteractionResponse.Private("That doesn't look like an incident id (e.g. INC-a1b2c3d4).");

        var incident = await mediator.Query(new GetIncidentQuery(id), ct);
        if (incident is null)
            return InteractionResponse.Private($"I can't find {IncidentId.Display(id)}.");

        var issue = incident.GitHubIssueUrl is null ? "issue pending" : incident.GitHubIssueUrl;
        var resolution = incident.Status == IncidentStatus.Resolved
            ? $"resolved in {Format.Duration(incident.TimeToResolveMinutes)}"
            : "open";

        return InteractionResponse.Private(
            $"**{IncidentId.Display(incident.Id)}** — {incident.Severity.Label()} — {resolution}\n" +
            $"{incident.Title}\nReported by {incident.ReportedBy} • {issue}");
    }

    private async Task<InteractionResponse> StatsAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var report = await mediator.Query(new GetAnalyticsQuery(now.AddDays(-30), now), ct);

        var lines = report.BySeverity
            .Where(s => s.Total > 0)
            .Select(s => $"• {s.Severity.Label()}: {s.Total} ({s.Open} open) — MTTR {Format.Duration(s.MeanTimeToResolveMinutes)}");

        var text = $"📊 **Last 30 days**: {report.Total} incidents, {report.Open} open, {report.Resolved} resolved. " +
                   $"MTTR {Format.Duration(report.MeanTimeToResolveMinutes)}";
        var breakdown = string.Join('\n', lines);

        return InteractionResponse.Private(breakdown.Length == 0 ? text : $"{text}\n{breakdown}");
    }

    private static string? OptionString(DiscordOption parent, string name)
    {
        var option = parent.Options?.FirstOrDefault(o => o.Name == name);
        return option?.Value is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;
    }
}

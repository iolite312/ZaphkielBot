using global::Discord;
using global::Discord.Interactions;
using global::Discord.Rest;

namespace ZaphkielBot.Discord;

/// <summary>
/// The /incident slash commands (REST module base: the first response is returned as the HTTP response body). Deliberately thin: it only translates between Discord.Net and
/// <see cref="IIncidentCommandService"/>, which holds the actual logic.
/// </summary>
[Group("incident", "Report and track incidents")]
public sealed class IncidentModule(IIncidentCommandService incidents) : RestInteractionModuleBase<RestInteractionContext>
{
    [SlashCommand("report", "Report a new incident")]
    public Task ReportAsync(
        [Summary("title", "Short summary"), MinLength(3), MaxLength(100)] string title,
        [Summary("severity", "How bad is it?")]
        [Choice("SEV1 - Critical", "critical"), Choice("SEV2 - High", "high"), Choice("SEV3 - Medium", "medium"), Choice("SEV4 - Low", "low")]
        string severity,
        [Summary("description", "What is happening?"), MaxLength(1500)] string? description = null) =>
        ReplyAsync(incidents.ReportAsync(Invocation(), title, severity, description, CancellationToken.None));

    [SlashCommand("resolve", "Mark an incident as resolved")]
    public Task ResolveAsync([Summary("id", "Incident id, e.g. INC-a1b2c3d4")] string id) =>
        ReplyAsync(incidents.ResolveAsync(Invocation(), id, CancellationToken.None));

    [SlashCommand("status", "Show an incident")]
    public Task StatusAsync([Summary("id", "Incident id, e.g. INC-a1b2c3d4")] string id) =>
        ReplyAsync(incidents.StatusAsync(Invocation(), id, CancellationToken.None));

    [SlashCommand("stats", "Incident counts and MTTR for the last 30 days")]
    public Task StatsAsync() =>
        ReplyAsync(incidents.StatsAsync(Invocation(), CancellationToken.None));

    private InvocationContext Invocation() => new(
        Context.Interaction.ChannelId?.ToString() ?? string.Empty,
        Context.User.Id.ToString(),
        (Context.User as IGuildUser)?.Nickname ?? Context.User.GlobalName ?? Context.User.Username);

    private async Task ReplyAsync(Task<BotReply> reply)
    {
        var result = await reply;
        await RespondAsync(result.Content, ephemeral: result.Ephemeral, allowedMentions: AllowedMentions.None);
    }
}

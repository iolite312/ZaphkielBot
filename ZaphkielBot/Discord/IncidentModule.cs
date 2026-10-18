using Discord;
using Discord.Interactions;
using Discord.Rest;
using ZaphkielBot.Interfaces;

namespace ZaphkielBot.Discord;

[Group("incident", "Report and track incidents")]
public class IncidentModule(IIncidentCommandService incidents) : RestInteractionModuleBase<RestInteractionContext>
{
    [SlashCommand("report", "Report a new incident")]
    public Task ReportAsync(
        [Summary("title", "Short summary"), MinLength(3), MaxLength(100)] string title,
        [Summary("severity", "How bad is it?")]
        [Choice("Critical", "critical"), Choice("High", "high"), Choice("Medium", "medium"), Choice("Low", "low")]
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

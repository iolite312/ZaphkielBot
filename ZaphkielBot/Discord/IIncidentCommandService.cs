namespace ZaphkielBot.Discord;

/// <summary>Who ran a slash command and where. Plain values so the logic below does not depend on Discord.Net types.</summary>
public sealed record InvocationContext(string ChannelId, string UserId, string UserName);

/// <summary>What the bot answers. Ephemeral replies are only visible to the user who ran the command.</summary>
public sealed record BotReply(string Content, bool Ephemeral = false)
{
    public static BotReply Public(string content) => new(content);
    public static BotReply Private(string content) => new(content, Ephemeral: true);
}

/// <summary>Business logic behind the /incident slash commands.</summary>
public interface IIncidentCommandService
{
    Task<BotReply> ReportAsync(InvocationContext context, string? title, string? severity, string? description, CancellationToken cancellationToken);
    Task<BotReply> ResolveAsync(InvocationContext context, string? id, CancellationToken cancellationToken);
    Task<BotReply> StatusAsync(InvocationContext context, string? id, CancellationToken cancellationToken);
    Task<BotReply> StatsAsync(InvocationContext context, CancellationToken cancellationToken);
}

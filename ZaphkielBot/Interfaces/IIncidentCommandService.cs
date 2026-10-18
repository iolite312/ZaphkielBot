namespace ZaphkielBot.Interfaces;

public record InvocationContext(string ChannelId, string UserId, string UserName);

public record BotReply(string Content, bool Ephemeral = false)
{
    public static BotReply Public(string content) => new(content);
    public static BotReply Private(string content) => new(content, Ephemeral: true);
}

public interface IIncidentCommandService
{
    Task<BotReply> ReportAsync(InvocationContext context, string? title, string? severity, string? description, CancellationToken cancellationToken);
    Task<BotReply> ResolveAsync(InvocationContext context, string? id, CancellationToken cancellationToken);
    Task<BotReply> StatusAsync(InvocationContext context, string? id, CancellationToken cancellationToken);
    Task<BotReply> StatsAsync(InvocationContext context, CancellationToken cancellationToken);
}

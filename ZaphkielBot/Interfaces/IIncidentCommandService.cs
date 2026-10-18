using ZaphkielShared.Models;

namespace ZaphkielBot.Interfaces;

public record InvocationContext(string ChannelId, string UserId, string UserName);

public record BotReply(EmbedMessage Embed, bool Ephemeral = false)
{
    public static BotReply Public(EmbedMessage embed) => new(embed);
    public static BotReply Private(EmbedMessage embed) => new(embed, Ephemeral: true);

    public static BotReply Refused(string reason) =>
        Private(new EmbedMessage("⚠️ Can't do that", reason, Style: EmbedStyle.Warning));

    public static BotReply Failed() =>
        Private(new EmbedMessage("⚠️ Something went wrong", "Please try again in a moment.", Style: EmbedStyle.Danger));
}

public interface IIncidentCommandService
{
    Task<BotReply> ReportAsync(InvocationContext context, string? title, string? severity, string? description, CancellationToken cancellationToken);
    Task<BotReply> ResolveAsync(InvocationContext context, string? id, CancellationToken cancellationToken);
    Task<BotReply> StatusAsync(InvocationContext context, string? id, CancellationToken cancellationToken);
    Task<BotReply> StatsAsync(InvocationContext context, CancellationToken cancellationToken);
}

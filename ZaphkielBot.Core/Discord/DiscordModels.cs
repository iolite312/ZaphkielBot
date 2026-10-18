using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZaphkielBot.Core.Discord;

// Discord uses snake_case; see DiscordJson.Options.

public sealed record DiscordInteraction(
    int Type,
    string? ChannelId,
    DiscordMember? Member,
    DiscordUser? User,
    DiscordCommandData? Data);

public sealed record DiscordMember(DiscordUser? User, string? Nick);

public sealed record DiscordUser(string Id, string Username, string? GlobalName);

public sealed record DiscordCommandData(string Name, List<DiscordOption>? Options);

public sealed record DiscordOption(string Name, int Type, JsonElement? Value, List<DiscordOption>? Options);

public sealed record InteractionResponse(int Type, InteractionResponseData? Data = null)
{
    public const int TypePong = 1;
    public const int TypeChannelMessage = 4;
    public const int FlagEphemeral = 64;

    public static InteractionResponse Pong() => new(TypePong);

    public static InteractionResponse Message(string content) =>
        new(TypeChannelMessage, new InteractionResponseData(content));

    /// <summary>Only the invoking user sees the message.</summary>
    public static InteractionResponse Private(string content) =>
        new(TypeChannelMessage, new InteractionResponseData(content, FlagEphemeral));
}

public sealed record InteractionResponseData(
    string Content,
    int? Flags = null,
    AllowedMentions? AllowedMentions = null)
{
    /// <summary>No parse entries means nothing in the content can ping @everyone, roles or users.</summary>
    public AllowedMentions? AllowedMentions { get; init; } = AllowedMentions ?? new AllowedMentions([]);
}

public sealed record AllowedMentions(string[] Parse);

public static class DiscordJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

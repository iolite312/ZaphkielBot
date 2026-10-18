namespace ZaphkielShared.Models;

public enum EmbedStyle
{
    Neutral,
    Info,
    Success,
    Warning,
    Danger
}

public record EmbedField(string Name, string Value, bool Inline = true);

public record EmbedMessage(
    string Title,
    string? Description = null,
    string? Url = null,
    EmbedStyle Style = EmbedStyle.Info,
    IReadOnlyList<EmbedField>? Fields = null,
    string? Footer = null,
    DateTimeOffset? Timestamp = null);

public static class EmbedStyles
{
    public static EmbedStyle For(Severity severity) => severity switch
    {
        Severity.Critical => EmbedStyle.Danger,
        Severity.High => EmbedStyle.Warning,
        Severity.Medium => EmbedStyle.Info,
        _ => EmbedStyle.Neutral
    };
}

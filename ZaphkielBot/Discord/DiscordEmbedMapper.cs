using Discord;
using ZaphkielBot.Interfaces;
using ZaphkielCore.Formatting;
using ZaphkielShared.Models;

namespace ZaphkielBot.Discord;

public class DiscordEmbedMapper : IEmbedMapper
{
    public Embed Build(EmbedMessage message)
    {
        // Discord rejects embeds over 6000 characters in total, so every piece is fitted into what is left.
        var budget = EmbedBuilder.MaxEmbedLength;

        string Fit(string text, int max)
        {
            var fitted = MessageFormat.Truncate(text, Math.Max(1, Math.Min(max, budget)));
            budget -= fitted.Length;
            return fitted;
        }

        var builder = new EmbedBuilder()
            .WithTitle(Fit(message.Title, EmbedBuilder.MaxTitleLength))
            .WithColor(ColorFor(message.Style));

        if (!string.IsNullOrWhiteSpace(message.Footer))
            builder.WithFooter(Fit(message.Footer, EmbedFooterBuilder.MaxFooterTextLength));

        if (!string.IsNullOrWhiteSpace(message.Description) && budget > 0)
            builder.WithDescription(Fit(message.Description, EmbedBuilder.MaxDescriptionLength));

        if (Uri.TryCreate(message.Url, UriKind.Absolute, out var url) && url.Scheme is "http" or "https")
            builder.WithUrl(url.AbsoluteUri);

        foreach (var field in (message.Fields ?? []).Take(EmbedBuilder.MaxFieldCount))
        {
            if (budget < 4) break;

            var name = Fit(Text(field.Name), Math.Min(EmbedFieldBuilder.MaxFieldNameLength, budget - 1));
            var value = Fit(Text(field.Value), EmbedFieldBuilder.MaxFieldValueLength);
            builder.AddField(name, value, field.Inline);
        }

        if (message.Timestamp is { } timestamp)
            builder.WithTimestamp(timestamp);

        return builder.Build();
    }

    private static string Text(string? text) => string.IsNullOrWhiteSpace(text) ? "-" : text;

    private static Color ColorFor(EmbedStyle style) => style switch
    {
        EmbedStyle.Success => new Color(0x57F287),
        EmbedStyle.Warning => new Color(0xE67E22),
        EmbedStyle.Danger => new Color(0xED4245),
        EmbedStyle.Info => new Color(0x5865F2),
        _ => new Color(0x95A5A6)
    };
}

using System.ComponentModel.DataAnnotations;

namespace ZaphkielBot.Core.Discord;

public sealed class DiscordOptions
{
    public const string Section = "Discord";

    /// <summary>Ed25519 public key (hex) from the Discord developer portal. Used to authenticate every interaction.</summary>
    [Required, RegularExpression("^[0-9a-fA-F]{64}$", ErrorMessage = "Discord:PublicKey must be 64 hex characters.")]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Bot token used to post messages back to the channel.</summary>
    [Required]
    public string BotToken { get; set; } = string.Empty;

    /// <summary>When set, slash commands are only accepted from this channel (the designated incident channel).</summary>
    public string? IncidentChannelId { get; set; }
}

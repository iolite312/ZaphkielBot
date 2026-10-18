using System.ComponentModel.DataAnnotations;

namespace ZaphkielShared.Options;

public class DiscordOptions
{
    public const string Section = "Discord";

    [Required, RegularExpression("^[0-9a-fA-F]{64}$", ErrorMessage = "Discord:PublicKey must be 64 hex characters.")]
    public string PublicKey { get; set; } = string.Empty;

    [Required]
    public string BotToken { get; set; } = string.Empty;

    public string? IncidentChannelId { get; set; }

    public string? GuildId { get; set; }
}

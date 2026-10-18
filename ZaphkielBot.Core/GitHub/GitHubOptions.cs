using System.ComponentModel.DataAnnotations;

namespace ZaphkielBot.Core.GitHub;

public sealed class GitHubOptions
{
    public const string Section = "GitHub";

    /// <summary>Fine-grained personal access token with "Issues: read and write" on the target repository only.</summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string Owner { get; set; } = string.Empty;

    [Required]
    public string Repository { get; set; } = string.Empty;

    /// <summary>Shared secret configured on the repository webhook. Used to authenticate every delivery.</summary>
    [Required, MinLength(16)]
    public string WebhookSecret { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace ZaphkielShared.Options;

/// <summary>Settings for receiving GitHub webhooks (bot only). Shares the "GitHub" section with <see cref="GitHubOptions"/>.</summary>
public sealed class GitHubWebhookOptions
{
    public const string Section = "GitHub";

    /// <summary>Shared secret configured on the repository webhook. Authenticates every delivery.</summary>
    [Required, MinLength(16)]
    public string WebhookSecret { get; set; } = string.Empty;
}

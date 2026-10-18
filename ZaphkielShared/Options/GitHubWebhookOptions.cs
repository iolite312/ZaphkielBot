using System.ComponentModel.DataAnnotations;

namespace ZaphkielShared.Options;

public class GitHubWebhookOptions
{
    public const string Section = "GitHub";

    [Required, MinLength(16)]
    public string WebhookSecret { get; set; } = string.Empty;
}

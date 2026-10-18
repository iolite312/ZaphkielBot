using System.ComponentModel.DataAnnotations;

namespace ZaphkielShared.Options;

/// <summary>Settings for calling the GitHub API (processor only).</summary>
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
}

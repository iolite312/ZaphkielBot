using System.ComponentModel.DataAnnotations;

namespace ZaphkielShared.Options;

public class GitHubOptions
{
    public const string Section = "GitHub";

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string Owner { get; set; } = string.Empty;

    [Required]
    public string Repository { get; set; } = string.Empty;
}

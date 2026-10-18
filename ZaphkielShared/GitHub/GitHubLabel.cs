using ZaphkielShared.Models;

namespace ZaphkielShared.GitHub;

public record GitHubLabel(string Name, string Color, string Description);

public static class GitHubLabels
{
    public static readonly GitHubLabel Incident = new("incident", "7057FF", "Reported through the Discord incident bot");

    // Same palette as the Discord embeds, so a severity looks the same everywhere.
    public static GitHubLabel For(Severity severity) => new(
        $"Severity: {severity.Label()}",
        severity switch
        {
            Severity.Critical => "ED4245",
            Severity.High => "E67E22",
            Severity.Medium => "5865F2",
            _ => "95A5A6"
        },
        $"{severity.Label()} severity incident");
}

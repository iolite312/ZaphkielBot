namespace ZaphkielBot.Core.Domain;

/// <summary>Numeric value matches the usual SEV level (SEV1 is the most severe).</summary>
public enum Severity
{
    Critical = 1,
    High = 2,
    Medium = 3,
    Low = 4
}

public static class SeverityExtensions
{
    public static string Label(this Severity severity) => severity switch
    {
        Severity.Critical => "SEV1 - Critical",
        Severity.High => "SEV2 - High",
        Severity.Medium => "SEV3 - Medium",
        Severity.Low => "SEV4 - Low",
        _ => severity.ToString()
    };

    public static string GitHubLabel(this Severity severity) => $"sev{(int)severity}";

    public static bool TryParseSeverity(string? value, out Severity severity) =>
        Enum.TryParse(value, ignoreCase: true, out severity) && Enum.IsDefined(severity);
}

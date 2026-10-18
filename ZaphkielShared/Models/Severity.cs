namespace ZaphkielShared.Models;

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
        Severity.Critical => "Critical",
        Severity.High => "High",
        Severity.Medium => "Medium",
        Severity.Low => "Low",
        _ => severity.ToString()
    };

    public static bool TryParseSeverity(string? value, out Severity severity) =>
        Enum.TryParse(value, ignoreCase: true, out severity) && Enum.IsDefined(severity);
}

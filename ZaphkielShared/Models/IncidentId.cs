using System.Text.RegularExpressions;

namespace ZaphkielShared.Domain;

public static partial class IncidentId
{
    public const string Prefix = "INC-";

    [GeneratedRegex("^(?:INC-)?([0-9a-f]{8})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public static string New() => Guid.NewGuid().ToString("N")[..8];

    public static string Display(string id) => Prefix + id;

    public static string? Normalize(string? input)
    {
        var match = Pattern().Match(input?.Trim() ?? string.Empty);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
    }
}

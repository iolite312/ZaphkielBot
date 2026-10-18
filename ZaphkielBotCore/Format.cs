namespace ZaphkielBotCore;

internal static class Format
{
    public static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";

    public static string Duration(TimeSpan duration) => duration.TotalMinutes switch
    {
        < 1 => $"{duration.TotalSeconds:0}s",
        < 60 => $"{duration.TotalMinutes:0}m",
        < 60 * 24 => $"{(int)duration.TotalHours}h {duration.Minutes}m",
        _ => $"{(int)duration.TotalDays}d {duration.Hours}h"
    };

    public static string Duration(double? minutes) =>
        minutes is { } m ? Duration(TimeSpan.FromMinutes(m)) : "n/a";
}

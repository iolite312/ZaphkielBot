using ZaphkielBotCore.Domain;

namespace ZaphkielBot.Tests.Fakes;

public static class TestData
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public static Incident Incident(
        string id = "a1b2c3d4", Severity severity = Severity.High, DateTimeOffset? createdAt = null, int? issue = null)
    {
        var incident = ZaphkielBotCore.Domain.Incident.Report(
            id, "Database is down", "Connections time out", severity, "user-1", "Alice", "channel-1", createdAt ?? T0);
        if (issue is { } n) incident.LinkGitHubIssue(n, $"https://github.com/o/r/issues/{n}");
        return incident;
    }
}

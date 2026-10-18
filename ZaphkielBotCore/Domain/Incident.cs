namespace ZaphkielBotCore.Domain;

public sealed class Incident
{
    public const int MinTitleLength = 3;
    public const int MaxTitleLength = 100;
    public const int MaxDescriptionLength = 1500;

    public string Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public Severity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string ReporterId { get; private set; }
    public string ReporterName { get; private set; }
    public string ChannelId { get; private set; }
    public int? GitHubIssueNumber { get; private set; }
    public string? GitHubIssueUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public TimeSpan? TimeToResolve => ResolvedAt - CreatedAt;

    private Incident(
        string id, string title, string description, Severity severity, IncidentStatus status,
        string reporterId, string reporterName, string channelId,
        int? gitHubIssueNumber, string? gitHubIssueUrl,
        DateTimeOffset createdAt, DateTimeOffset? resolvedAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Severity = severity;
        Status = status;
        ReporterId = reporterId;
        ReporterName = reporterName;
        ChannelId = channelId;
        GitHubIssueNumber = gitHubIssueNumber;
        GitHubIssueUrl = gitHubIssueUrl;
        CreatedAt = createdAt;
        ResolvedAt = resolvedAt;
    }

    /// <summary>Returns a human readable validation error, or null when the input is acceptable.</summary>
    public static string? ValidateReport(string? title, string? description)
    {
        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length is < MinTitleLength or > MaxTitleLength)
            return $"The title must be between {MinTitleLength} and {MaxTitleLength} characters.";

        if ((description?.Trim().Length ?? 0) > MaxDescriptionLength)
            return $"The description can be at most {MaxDescriptionLength} characters.";

        return null;
    }

    public static Incident Report(
        string id, string title, string? description, Severity severity,
        string reporterId, string reporterName, string channelId, DateTimeOffset reportedAt)
    {
        var error = ValidateReport(title, description);
        if (error is not null) throw new ArgumentException(error, nameof(title));
        if (IncidentId.Normalize(id) != id) throw new ArgumentException("Invalid incident id.", nameof(id));
        if (!Enum.IsDefined(severity)) throw new ArgumentOutOfRangeException(nameof(severity));

        return new Incident(
            id, title.Trim(), description?.Trim() ?? string.Empty, severity, IncidentStatus.Open,
            reporterId, reporterName, channelId, null, null, reportedAt.ToUniversalTime(), null);
    }

    /// <summary>Rebuilds an incident from persisted state without re-running the reporting rules.</summary>
    public static Incident Rehydrate(
        string id, string title, string description, Severity severity, IncidentStatus status,
        string reporterId, string reporterName, string channelId,
        int? gitHubIssueNumber, string? gitHubIssueUrl,
        DateTimeOffset createdAt, DateTimeOffset? resolvedAt) =>
        new(id, title, description, severity, status, reporterId, reporterName, channelId,
            gitHubIssueNumber, gitHubIssueUrl, createdAt, resolvedAt);

    public void LinkGitHubIssue(int number, string url)
    {
        GitHubIssueNumber = number;
        GitHubIssueUrl = url;
    }

    /// <returns>true when the incident transitioned to resolved, false when it already was.</returns>
    public bool Resolve(DateTimeOffset resolvedAt)
    {
        if (Status == IncidentStatus.Resolved) return false;

        Status = IncidentStatus.Resolved;
        var at = resolvedAt.ToUniversalTime();
        ResolvedAt = at < CreatedAt ? CreatedAt : at;
        return true;
    }
}

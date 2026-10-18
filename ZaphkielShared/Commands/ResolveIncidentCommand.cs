using ZaphkielShared.Messaging;

namespace ZaphkielShared.Commands;

public enum ResolutionSource
{
    Discord,
    GitHub
}

public record ResolveIncidentCommand(
    string? IncidentId,
    int? GitHubIssueNumber,
    string ResolvedBy,
    ResolutionSource Source,
    DateTimeOffset ResolvedAt) : IQueuedCommand;

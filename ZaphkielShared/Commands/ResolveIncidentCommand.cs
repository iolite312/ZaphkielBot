using ZaphkielShared.Messaging;

namespace ZaphkielShared.Commands;

public enum ResolutionSource
{
    Discord,
    GitHub
}

/// <summary>Identify the incident either by its id (Discord) or by its GitHub issue number (webhook).</summary>
public sealed record ResolveIncidentCommand(
    string? IncidentId,
    int? GitHubIssueNumber,
    string ResolvedBy,
    ResolutionSource Source,
    DateTimeOffset ResolvedAt) : IQueuedCommand;

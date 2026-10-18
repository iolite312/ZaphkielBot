using System.Text.Json;

namespace ZaphkielShared.GitHub;

public sealed record NewGitHubIssue(string Title, string Body, IReadOnlyList<string> Labels);

public sealed record GitHubIssue(int Number, string Url);

// Webhook payloads: only the fields we use. GitHub sends snake_case.
public sealed record GitHubWebhookPayload(
    string? Action,
    GitHubIssuePayload? Issue,
    GitHubCommentPayload? Comment,
    GitHubUserPayload? Sender);

public sealed record GitHubIssuePayload(int Number, JsonElement? PullRequest);

public sealed record GitHubCommentPayload(long Id, string? Body, string HtmlUrl, GitHubUserPayload User);

public sealed record GitHubUserPayload(string Login, string? Type);

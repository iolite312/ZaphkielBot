using System.Text.Json;

namespace ZaphkielShared.GitHub;

public record NewGitHubIssue(string Title, string Body, IReadOnlyList<GitHubLabel> Labels);

public record GitHubIssue(int Number, string Url);

// Webhook payloads: only the fields we use. GitHub sends snake_case.
public record GitHubWebhookPayload(
    string? Action,
    GitHubIssuePayload? Issue,
    GitHubCommentPayload? Comment,
    GitHubUserPayload? Sender);

public record GitHubIssuePayload(int Number, JsonElement? PullRequest);

public record GitHubCommentPayload(long Id, string? Body, string HtmlUrl, GitHubUserPayload User);

public record GitHubUserPayload(string Login, string? Type);

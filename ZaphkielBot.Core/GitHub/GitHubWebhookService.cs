using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZaphkielBot.Core.Abstractions;
using ZaphkielBot.Core.Commands;
using ZaphkielBot.Core.Messaging;

namespace ZaphkielBot.Core.GitHub;

public enum WebhookOutcome
{
    /// <summary>The delivery was understood and (where relevant) queued.</summary>
    Accepted,
    Unauthorized,
    BadRequest
}

public interface IGitHubWebhookService
{
    Task<WebhookOutcome> HandleAsync(string? eventName, string? signature, byte[] body, CancellationToken cancellationToken);
}

/// <summary>Authenticates GitHub webhook deliveries and turns relevant ones into queued commands.</summary>
public sealed class GitHubWebhookService(
    IGitHubSignatureVerifier signatureVerifier,
    ICommandQueue queue,
    TimeProvider clock,
    ILogger<GitHubWebhookService> logger) : IGitHubWebhookService
{
    public async Task<WebhookOutcome> HandleAsync(
        string? eventName, string? signature, byte[] body, CancellationToken cancellationToken)
    {
        if (!signatureVerifier.Verify(body, signature))
        {
            logger.LogWarning("Rejected a GitHub webhook with an invalid signature");
            return WebhookOutcome.Unauthorized;
        }

        try
        {
            switch (eventName)
            {
                case "issue_comment":
                    await HandleCommentAsync(Parse(body), cancellationToken);
                    break;
                case "issues":
                    await HandleIssueAsync(Parse(body), cancellationToken);
                    break;
                default: // includes "ping"
                    logger.LogInformation("Ignoring GitHub event {Event}", eventName);
                    break;
            }
        }
        catch (JsonException)
        {
            return WebhookOutcome.BadRequest;
        }

        return WebhookOutcome.Accepted;
    }

    private async Task HandleCommentAsync(GitHubWebhookPayload payload, CancellationToken ct)
    {
        if (payload is not { Action: "created", Issue: { } issue, Comment: { } comment })
            return;

        // Pull requests also raise issue_comment events; bots (CI, dependabot...) would only add noise in Discord.
        if (issue.PullRequest is not null || comment.User.Type == "Bot")
            return;

        await queue.EnqueueAsync(
            QueueNames.GitHubEvents,
            new RecordGitHubCommentCommand(issue.Number, comment.Id, comment.User.Login, comment.Body ?? string.Empty, comment.HtmlUrl),
            ct);
    }

    private async Task HandleIssueAsync(GitHubWebhookPayload payload, CancellationToken ct)
    {
        if (payload is not { Action: "closed", Issue: { } issue })
            return;

        await queue.EnqueueAsync(
            QueueNames.GitHubEvents,
            new ResolveIncidentCommand(null, issue.Number, payload.Sender?.Login ?? "GitHub", ResolutionSource.GitHub, clock.GetUtcNow()),
            ct);
    }

    private static GitHubWebhookPayload Parse(byte[] body) =>
        JsonSerializer.Deserialize<GitHubWebhookPayload>(body, GitHubJson.Options)
        ?? throw new JsonException("Empty payload.");
}

// Only the fields we use. GitHub uses snake_case.
public sealed record GitHubWebhookPayload(
    string? Action,
    GitHubIssuePayload? Issue,
    GitHubCommentPayload? Comment,
    GitHubUserPayload? Sender);

public sealed record GitHubIssuePayload(int Number, JsonElement? PullRequest);

public sealed record GitHubCommentPayload(long Id, string? Body, string HtmlUrl, GitHubUserPayload User);

public sealed record GitHubUserPayload(string Login, string? Type);

public static class GitHubJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };
}

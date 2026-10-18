using ZaphkielShared.GitHub;

namespace ZaphkielCore.Abstractions;

public interface IGitHubIssueClient
{
    Task<GitHubIssue> CreateIssueAsync(NewGitHubIssue issue, CancellationToken cancellationToken);
    Task CloseIssueAsync(int issueNumber, CancellationToken cancellationToken);
}

/// <summary>Asks for a message to appear in a Discord channel. Delivery is asynchronous (see the bot's queue function).</summary>
public interface IDiscordNotifier
{
    Task PostMessageAsync(string channelId, string content, CancellationToken cancellationToken);
}

/// <summary>Verifies the HMAC-SHA256 signature (X-Hub-Signature-256) GitHub attaches to every webhook delivery.</summary>
public interface IGitHubSignatureVerifier
{
    bool Verify(ReadOnlySpan<byte> body, string? signatureHeader);
}

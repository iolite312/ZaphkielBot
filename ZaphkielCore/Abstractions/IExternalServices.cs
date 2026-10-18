using ZaphkielShared.GitHub;

namespace ZaphkielCore.Abstractions;

public interface IGitHubIssueClient
{
    Task<GitHubIssue> CreateIssueAsync(NewGitHubIssue issue, CancellationToken cancellationToken);
    Task CloseIssueAsync(int issueNumber, CancellationToken cancellationToken);
}

public interface IDiscordNotifier
{
    Task PostMessageAsync(string channelId, string content, CancellationToken cancellationToken);
}

public interface IGitHubSignatureVerifier
{
    bool Verify(ReadOnlySpan<byte> body, string? signatureHeader);
}

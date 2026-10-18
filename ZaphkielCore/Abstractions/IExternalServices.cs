using ZaphkielShared.GitHub;
using ZaphkielShared.Models;

namespace ZaphkielCore.Abstractions;

public interface IGitHubIssueClient
{
    Task<GitHubIssue> CreateIssueAsync(NewGitHubIssue issue, CancellationToken cancellationToken);
    Task CloseIssueAsync(int issueNumber, CancellationToken cancellationToken);
}

public interface IDiscordNotifier
{
    Task PostEmbedAsync(string channelId, EmbedMessage embed, CancellationToken cancellationToken);
}

public interface IGitHubSignatureVerifier
{
    bool Verify(ReadOnlySpan<byte> body, string? signatureHeader);
}

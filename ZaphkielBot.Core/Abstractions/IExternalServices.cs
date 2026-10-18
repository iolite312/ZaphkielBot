namespace ZaphkielBot.Core.Abstractions;

public sealed record NewGitHubIssue(string Title, string Body, IReadOnlyList<string> Labels);

public sealed record GitHubIssue(int Number, string Url);

public interface IGitHubIssueClient
{
    Task<GitHubIssue> CreateIssueAsync(NewGitHubIssue issue, CancellationToken cancellationToken);
    Task CloseIssueAsync(int issueNumber, CancellationToken cancellationToken);
}

public interface IDiscordNotifier
{
    Task PostMessageAsync(string channelId, string content, CancellationToken cancellationToken);
}

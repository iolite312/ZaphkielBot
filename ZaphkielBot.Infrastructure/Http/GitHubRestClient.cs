using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZaphkielBot.Core.Abstractions;
using ZaphkielBot.Core.GitHub;

namespace ZaphkielBot.Infrastructure.Http;

public sealed class GitHubRestClient(HttpClient http, IOptions<GitHubOptions> options) : IGitHubIssueClient
{
    private readonly GitHubOptions _options = options.Value;

    private string IssuesPath =>
        $"repos/{Uri.EscapeDataString(_options.Owner)}/{Uri.EscapeDataString(_options.Repository)}/issues";

    public async Task<GitHubIssue> CreateIssueAsync(NewGitHubIssue issue, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(
            IssuesPath, new { title = issue.Title, body = issue.Body, labels = issue.Labels }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return new GitHubIssue(json.RootElement.GetProperty("number").GetInt32(), json.RootElement.GetProperty("html_url").GetString()!);
    }

    public async Task CloseIssueAsync(int issueNumber, CancellationToken cancellationToken)
    {
        using var response = await http.PatchAsJsonAsync(
            $"{IssuesPath}/{issueNumber}", new { state = "closed", state_reason = "completed" }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        // Include GitHub's error text but never the request headers (which carry the token).
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"GitHub API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
    }
}

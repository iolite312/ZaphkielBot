using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZaphkielCore.Abstractions;
using ZaphkielShared.GitHub;
using ZaphkielShared.Options;

namespace ZaphkielProcessor.GitHub;

public class GitHubRestClient : IGitHubIssueClient
{
    private readonly HttpClient _http;
    private readonly string _issuesPath;

    public GitHubRestClient(HttpClient http, IOptions<GitHubOptions> options)
    {
        var settings = options.Value;
        _http = http;
        _issuesPath = $"repos/{Uri.EscapeDataString(settings.Owner)}/{Uri.EscapeDataString(settings.Repository)}/issues";

        http.BaseAddress = new Uri("https://api.github.com/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.Token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ZaphkielBot", "1.0"));
        http.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<GitHubIssue> CreateIssueAsync(NewGitHubIssue issue, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(
            _issuesPath, new { title = issue.Title, body = issue.Body, labels = issue.Labels }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return new GitHubIssue(json.RootElement.GetProperty("number").GetInt32(), json.RootElement.GetProperty("html_url").GetString()!);
    }

    public async Task CloseIssueAsync(int issueNumber, CancellationToken cancellationToken)
    {
        using var response = await _http.PatchAsJsonAsync(
            $"{_issuesPath}/{issueNumber}", new { state = "closed", state_reason = "completed" }, cancellationToken);
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

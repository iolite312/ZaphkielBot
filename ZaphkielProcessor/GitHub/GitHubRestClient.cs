using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZaphkielCore.Abstractions;
using ZaphkielShared.GitHub;
using ZaphkielShared.Options;

namespace ZaphkielProcessor.GitHub;

public class GitHubRestClient : IGitHubIssueClient
{
    // Labels already created/coloured by this process, so it happens once instead of on every incident.
    private static readonly ConcurrentDictionary<string, bool> EnsuredLabels = new();

    private readonly HttpClient _http;
    private readonly ILogger<GitHubRestClient> _logger;
    private readonly string _repoPath;
    private readonly string _issuesPath;

    public GitHubRestClient(HttpClient http, IOptions<GitHubOptions> options, ILogger<GitHubRestClient> logger)
    {
        var settings = options.Value;
        _http = http;
        _logger = logger;
        _repoPath = $"repos/{Uri.EscapeDataString(settings.Owner)}/{Uri.EscapeDataString(settings.Repository)}";
        _issuesPath = $"{_repoPath}/issues";

        http.BaseAddress = new Uri("https://api.github.com/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.Token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ZaphkielBot", "1.0"));
        http.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<GitHubIssue> CreateIssueAsync(NewGitHubIssue issue, CancellationToken cancellationToken)
    {
        foreach (var label in issue.Labels)
            await EnsureLabelAsync(label, cancellationToken);

        using var response = await _http.PostAsJsonAsync(
            _issuesPath, new { title = issue.Title, body = issue.Body, labels = issue.Labels.Select(l => l.Name) }, cancellationToken);
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

    private async Task EnsureLabelAsync(GitHubLabel label, CancellationToken cancellationToken)
    {
        if (EnsuredLabels.ContainsKey(label.Name)) return;

        try
        {
            var details = new { name = label.Name, color = label.Color, description = label.Description };
            using var created = await _http.PostAsJsonAsync($"{_repoPath}/labels", details, cancellationToken);

            if (created.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                // Already exists (e.g. created uncoloured by an earlier issue): apply our colour and description.
                using var updated = await _http.PatchAsJsonAsync(
                    $"{_repoPath}/labels/{Uri.EscapeDataString(label.Name)}",
                    new { color = label.Color, description = label.Description },
                    cancellationToken);
                await EnsureSuccessAsync(updated, cancellationToken);
            }
            else
            {
                await EnsureSuccessAsync(created, cancellationToken);
            }

            EnsuredLabels[label.Name] = true;
        }
        catch (HttpRequestException ex)
        {
            // Colours are cosmetic: GitHub still creates unknown labels when the issue is opened.
            _logger.LogWarning(ex, "Could not create or update the GitHub label {Label}", label.Name);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        // Include GitHub's error text but never the request headers (which carry the token).
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"GitHub API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
    }
}

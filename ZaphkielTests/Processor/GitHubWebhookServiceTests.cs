using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using ZaphkielProcessor.GitHub;
using ZaphkielShared.Commands;
using ZaphkielShared.Messaging;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Processor;

public class GitHubWebhookServiceTests
{
    private readonly StubGitHubSignatureVerifier _verifier = new();
    private readonly RecordingCommandQueue _queue = new();

    private IGitHubWebhookService Service =>
        new GitHubWebhookService(_verifier, _queue, new FixedClock(), NullLogger<GitHubWebhookService>.Instance);

    private Task<WebhookOutcome> Send(string eventName, string json) =>
        Service.HandleAsync(eventName, "sha256=x", Encoding.UTF8.GetBytes(json), CancellationToken.None);

    private static string Comment(string action = "created", string userType = "User", string issueExtra = "") => $$$"""
        {"action":"{{{action}}}","issue":{"number":7{{{issueExtra}}}},
         "comment":{"id":99,"body":"Looking into it","html_url":"https://github.com/o/r/issues/7#c99","user":{"login":"dev-jane","type":"{{{userType}}}"}},
         "sender":{"login":"dev-jane"}}
        """;

    [Fact]
    public async Task Invalid_signature_is_unauthorized_and_nothing_is_queued()
    {
        _verifier.Result = false;

        Assert.Equal(WebhookOutcome.Unauthorized, await Send("issue_comment", Comment()));
        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task New_comment_is_queued_on_the_github_events_queue()
    {
        Assert.Equal(WebhookOutcome.Accepted, await Send("issue_comment", Comment()));

        var (queueName, queued) = Assert.Single(_queue.Sent);
        Assert.Equal(QueueNames.GitHubEvents, queueName);
        var command = Assert.IsType<RecordGitHubCommentCommand>(queued);
        Assert.Equal(7, command.IssueNumber);
        Assert.Equal("dev-jane", command.Author);
        Assert.Equal("Looking into it", command.Body);
        Assert.Equal("https://github.com/o/r/issues/7#c99", command.Url);
    }

    [Theory]
    [InlineData("edited", "User", "")]
    [InlineData("deleted", "User", "")]
    [InlineData("created", "Bot", "")]
    [InlineData("created", "User", ",\"pull_request\":{}")]
    public async Task Edits_deletions_bots_and_pull_request_comments_are_ignored(string action, string userType, string issueExtra)
    {
        Assert.Equal(WebhookOutcome.Accepted, await Send("issue_comment", Comment(action, userType, issueExtra)));
        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Closed_issue_queues_a_resolve_command()
    {
        await Send("issues", """{"action":"closed","issue":{"number":7},"sender":{"login":"dev-jane"}}""");

        var command = Assert.IsType<ResolveIncidentCommand>(Assert.Single(_queue.Sent).Command);
        Assert.Equal(7, command.GitHubIssueNumber);
        Assert.Null(command.IncidentId);
        Assert.Equal("dev-jane", command.ResolvedBy);
        Assert.Equal(ResolutionSource.GitHub, command.Source);
    }

    [Theory]
    [InlineData("issues", """{"action":"opened","issue":{"number":7}}""")]
    [InlineData("ping", """{"zen":"Keep it logically awesome."}""")]
    [InlineData("push", """{}""")]
    public async Task Other_events_are_acknowledged_but_ignored(string eventName, string json)
    {
        Assert.Equal(WebhookOutcome.Accepted, await Send(eventName, json));
        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Malformed_json_is_a_bad_request() =>
        Assert.Equal(WebhookOutcome.BadRequest, await Send("issue_comment", "{nope"));
}

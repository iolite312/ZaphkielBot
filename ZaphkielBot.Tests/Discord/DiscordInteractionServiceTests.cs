using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZaphkielBot.Core.Abstractions;
using ZaphkielBot.Core.Commands;
using ZaphkielBot.Core.Cqrs;
using ZaphkielBot.Core.Discord;
using ZaphkielBot.Core.Domain;
using ZaphkielBot.Core.Messaging;
using ZaphkielBot.Core.Queries;
using ZaphkielBot.Tests.Fakes;

namespace ZaphkielBot.Tests.Discord;

public class DiscordInteractionServiceTests
{
    private readonly StubSignatureVerifier _verifier = new();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly RecordingCommandQueue _queue = new();
    private readonly DiscordOptions _options = new() { IncidentChannelId = "channel-1" };

    private DiscordInteractionService Service() => new(
        _verifier, _mediator, _queue, Options.Create(_options),
        new FixedClock(TestData.T0), NullLogger<DiscordInteractionService>.Instance);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static byte[] Command(string sub, object[]? options = null, string channel = "channel-1") =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = 2,
            channel_id = channel,
            member = new { nick = (string?)null, user = new { id = "user-1", username = "alice", global_name = "Alice" } },
            data = new { name = "incident", options = new[] { new { name = sub, type = 1, options = options ?? [] } } }
        });

    private static object Opt(string name, string value) => new { name, type = 3, value };

    private Task<InteractionResult> Send(byte[] body) => Service().HandleAsync("sig", "ts", body, CancellationToken.None);

    [Fact]
    public async Task Invalid_signature_is_unauthorized_and_nothing_is_queued()
    {
        _verifier.Result = false;

        var result = await Send(Command("report"));

        Assert.Equal(InteractionOutcome.Unauthorized, result.Outcome);
        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Ping_gets_a_pong()
    {
        var result = await Send(Encoding.UTF8.GetBytes("""{"type":1}"""));

        Assert.Equal(InteractionOutcome.Ok, result.Outcome);
        Assert.Equal(InteractionResponse.TypePong, result.Response!.Type);
    }

    [Fact]
    public async Task Malformed_json_is_a_bad_request()
    {
        var result = await Send(Encoding.UTF8.GetBytes("{not json"));

        Assert.Equal(InteractionOutcome.BadRequest, result.Outcome);
    }

    [Fact]
    public async Task Report_queues_a_create_command_and_acknowledges_publicly_with_the_new_id()
    {
        var result = await Send(Command("report", [Opt("title", "  Checkout is down "), Opt("severity", "critical"), Opt("description", "500s everywhere")]));

        var (queueName, queued) = Assert.Single(_queue.Sent);
        Assert.Equal(QueueNames.IncidentCommands, queueName);
        var command = Assert.IsType<CreateIncidentCommand>(queued);
        Assert.Equal("Checkout is down", command.Title);
        Assert.Equal(Severity.Critical, command.Severity);
        Assert.Equal("user-1", command.ReporterId);
        Assert.Equal("Alice", command.ReporterName);
        Assert.Equal("channel-1", command.ChannelId);
        Assert.Equal(TestData.T0, command.ReportedAt);

        Assert.Null(result.Response!.Data!.Flags); // public
        Assert.Contains(IncidentId.Display(command.IncidentId), result.Response.Data.Content);
        Assert.Empty(result.Response.Data.AllowedMentions!.Parse);
    }

    [Theory]
    [InlineData("ab", "high")]          // title too short
    [InlineData("A fine title", "urgent")] // unknown severity
    public async Task Invalid_report_is_rejected_privately_and_not_queued(string title, string severity)
    {
        var result = await Send(Command("report", [Opt("title", title), Opt("severity", severity)]));

        Assert.Equal(InteractionResponse.FlagEphemeral, result.Response!.Data!.Flags);
        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Commands_from_other_channels_are_refused()
    {
        var result = await Send(Command("report", [Opt("title", "Valid title"), Opt("severity", "low")], channel: "general"));

        Assert.Contains("<#channel-1>", result.Response!.Data!.Content);
        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Queue_failure_gives_a_friendly_private_error()
    {
        var failing = Substitute.For<ICommandQueue>();
        failing.EnqueueAsync(default!, default!, default).ReturnsForAnyArgs(_ => throw new InvalidOperationException("queue down"));
        var service = new DiscordInteractionService(_verifier, _mediator, failing, Options.Create(_options), new FixedClock(TestData.T0), NullLogger<DiscordInteractionService>.Instance);

        var result = await service.HandleAsync("sig", "ts", Command("report", [Opt("title", "Valid title"), Opt("severity", "low")]), CancellationToken.None);

        Assert.Equal(InteractionResponse.FlagEphemeral, result.Response!.Data!.Flags);
    }

    [Fact]
    public async Task Resolve_of_an_open_incident_is_queued()
    {
        _mediator.Query(Arg.Any<GetIncidentQuery>(), Arg.Any<CancellationToken>())
            .Returns(IncidentView.From(TestData.Incident()));

        await Send(Command("resolve", [Opt("id", "INC-A1B2C3D4")]));

        var command = Assert.IsType<ResolveIncidentCommand>(Assert.Single(_queue.Sent).Command);
        Assert.Equal("a1b2c3d4", command.IncidentId);
        Assert.Equal(ResolutionSource.Discord, command.Source);
    }

    [Fact]
    public async Task Resolve_of_unknown_or_already_resolved_incident_is_refused()
    {
        _mediator.Query(Arg.Any<GetIncidentQuery>(), Arg.Any<CancellationToken>()).Returns((IncidentView?)null);
        var unknown = await Send(Command("resolve", [Opt("id", "a1b2c3d4")]));
        Assert.Contains("can't find", unknown.Response!.Data!.Content);

        var resolved = TestData.Incident();
        resolved.Resolve(TestData.T0.AddMinutes(5));
        _mediator.Query(Arg.Any<GetIncidentQuery>(), Arg.Any<CancellationToken>()).Returns(IncidentView.From(resolved));
        var already = await Send(Command("resolve", [Opt("id", "a1b2c3d4")]));
        Assert.Contains("already resolved", already.Response!.Data!.Content);

        var malformed = await Send(Command("resolve", [Opt("id", "nope")]));
        Assert.Contains("doesn't look like", malformed.Response!.Data!.Content);

        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Stats_are_answered_from_the_query_side_without_queueing()
    {
        _mediator.Query(Arg.Any<GetAnalyticsQuery>(), Arg.Any<CancellationToken>()).Returns(new AnalyticsReport(
            TestData.T0.AddDays(-30), TestData.T0, 3, 1, 2, 45,
            [new SeverityStats(Severity.High, 3, 1, 2, 45)]));

        var result = await Send(Command("stats"));

        Assert.Contains("3 incidents", result.Response!.Data!.Content);
        Assert.Contains("45m", result.Response.Data.Content);
        Assert.Empty(_queue.Sent);
    }
}

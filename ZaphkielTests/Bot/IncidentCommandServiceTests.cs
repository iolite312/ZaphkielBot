using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZaphkielBot.Discord;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielShared.Commands;
using ZaphkielShared.Domain;
using ZaphkielShared.Messaging;
using ZaphkielShared.Options;
using ZaphkielShared.Queries;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Bot;

public class IncidentCommandServiceTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly RecordingCommandQueue _queue = new();
    private readonly DiscordOptions _options = new() { IncidentChannelId = "channel-1" };

    private static readonly InvocationContext Alice = new("channel-1", "user-1", "Alice");

    private IIncidentCommandService Service => Create(_queue);

    private IIncidentCommandService Create(ICommandQueue queue) => new IncidentCommandService(
        _mediator, queue, Options.Create(_options), new FixedClock(), NullLogger<IncidentCommandService>.Instance);

    [Fact]
    public async Task Report_queues_a_create_command_and_acknowledges_publicly_with_the_new_id()
    {
        var reply = await Service.ReportAsync(Alice, "  Checkout is down ", "critical", "500s everywhere", CancellationToken.None);

        var (queueName, queued) = Assert.Single(_queue.Sent);
        Assert.Equal(QueueNames.IncidentCommands, queueName);
        var command = Assert.IsType<CreateIncidentCommand>(queued);
        Assert.Equal("Checkout is down", command.Title);
        Assert.Equal(Severity.Critical, command.Severity);
        Assert.Equal("user-1", command.ReporterId);
        Assert.Equal("Alice", command.ReporterName);
        Assert.Equal("channel-1", command.ChannelId);
        Assert.Equal(TestData.T0, command.ReportedAt);

        Assert.False(reply.Ephemeral);
        Assert.Contains(IncidentId.Display(command.IncidentId), reply.Content);
    }

    [Theory]
    [InlineData("ab", "high")]             // title too short
    [InlineData("A fine title", "urgent")] // unknown severity
    [InlineData(null, "high")]
    public async Task Invalid_report_is_rejected_privately_and_not_queued(string? title, string severity)
    {
        var reply = await Service.ReportAsync(Alice, title, severity, null, CancellationToken.None);

        Assert.True(reply.Ephemeral);
        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Commands_from_other_channels_are_refused()
    {
        var elsewhere = Alice with { ChannelId = "general" };

        var reports = await Service.ReportAsync(elsewhere, "Valid title", "low", null, CancellationToken.None);
        var stats = await Service.StatsAsync(elsewhere, CancellationToken.None);

        Assert.All(new[] { reports, stats }, r =>
        {
            Assert.True(r.Ephemeral);
            Assert.Contains("<#channel-1>", r.Content);
        });
        Assert.Empty(_queue.Sent);
        await _mediator.DidNotReceiveWithAnyArgs().Query<AnalyticsReport>(default!, default);
    }

    [Fact]
    public async Task Any_channel_is_accepted_when_no_designated_channel_is_configured()
    {
        _options.IncidentChannelId = null;

        var reply = await Service.ReportAsync(Alice with { ChannelId = "anywhere" }, "Valid title", "low", null, CancellationToken.None);

        Assert.False(reply.Ephemeral);
    }

    [Fact]
    public async Task Report_without_a_known_channel_is_refused_because_it_could_never_be_announced()
    {
        _options.IncidentChannelId = null;

        var reply = await Service.ReportAsync(Alice with { ChannelId = "" }, "Valid title", "low", null, CancellationToken.None);

        Assert.True(reply.Ephemeral);
        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Queue_failure_gives_a_friendly_private_error()
    {
        var failing = Substitute.For<ICommandQueue>();
        failing.EnqueueAsync(default!, default!, default).ReturnsForAnyArgs(_ => throw new InvalidOperationException("queue down"));

        var reply = await Create(failing).ReportAsync(Alice, "Valid title", "low", null, CancellationToken.None);

        Assert.True(reply.Ephemeral);
        Assert.Contains("went wrong", reply.Content);
    }

    [Fact]
    public async Task Resolve_of_an_open_incident_is_queued()
    {
        _mediator.Query(Arg.Any<GetIncidentQuery>(), Arg.Any<CancellationToken>())
            .Returns(IncidentView.From(TestData.Incident()));

        var reply = await Service.ResolveAsync(Alice, "INC-A1B2C3D4", CancellationToken.None);

        var command = Assert.IsType<ResolveIncidentCommand>(Assert.Single(_queue.Sent).Command);
        Assert.Equal("a1b2c3d4", command.IncidentId);
        Assert.Equal("Alice", command.ResolvedBy);
        Assert.Equal(ResolutionSource.Discord, command.Source);
        Assert.False(reply.Ephemeral);
    }

    [Fact]
    public async Task Resolve_of_unknown_resolved_or_malformed_incident_is_refused()
    {
        _mediator.Query(Arg.Any<GetIncidentQuery>(), Arg.Any<CancellationToken>()).Returns((IncidentView?)null);
        var unknown = await Service.ResolveAsync(Alice, "a1b2c3d4", CancellationToken.None);
        Assert.Contains("can't find", unknown.Content);

        var resolved = TestData.Incident();
        resolved.Resolve(TestData.T0.AddMinutes(5));
        _mediator.Query(Arg.Any<GetIncidentQuery>(), Arg.Any<CancellationToken>()).Returns(IncidentView.From(resolved));
        var already = await Service.ResolveAsync(Alice, "a1b2c3d4", CancellationToken.None);
        Assert.Contains("already resolved", already.Content);

        var malformed = await Service.ResolveAsync(Alice, "nope", CancellationToken.None);
        Assert.Contains("doesn't look like", malformed.Content);

        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Status_shows_severity_state_and_issue_link_privately()
    {
        _mediator.Query(Arg.Any<GetIncidentQuery>(), Arg.Any<CancellationToken>())
            .Returns(IncidentView.From(TestData.Incident(issue: 7)));

        var reply = await Service.StatusAsync(Alice, "a1b2c3d4", CancellationToken.None);

        Assert.True(reply.Ephemeral);
        Assert.Contains("SEV2 - High", reply.Content);
        Assert.Contains("open", reply.Content);
        Assert.Contains("https://github.com/o/r/issues/7", reply.Content);
        Assert.Empty(_queue.Sent);
    }

    [Fact]
    public async Task Stats_are_answered_from_the_query_side_without_queueing()
    {
        _mediator.Query(Arg.Any<GetAnalyticsQuery>(), Arg.Any<CancellationToken>()).Returns(new AnalyticsReport(
            TestData.T0.AddDays(-30), TestData.T0, 3, 1, 2, 45,
            [new SeverityStats(Severity.High, 3, 1, 2, 45)]));

        var reply = await Service.StatsAsync(Alice, CancellationToken.None);

        Assert.Contains("3 incidents", reply.Content);
        Assert.Contains("45m", reply.Content);
        Assert.Empty(_queue.Sent);
    }
}

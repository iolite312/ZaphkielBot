using ZaphkielCore.Queries;
using ZaphkielShared.Domain;
using ZaphkielShared.Queries;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Core;

public class GetAnalyticsHandlerTests
{
    private readonly InMemoryIncidentRepository _repository = new();

    private GetAnalyticsHandler Handler => new(_repository);

    private static GetAnalyticsQuery Period => new(TestData.T0.AddDays(-1), TestData.T0.AddDays(1));

    [Fact]
    public async Task Computes_counts_and_mean_time_to_resolve_per_severity()
    {
        var a = TestData.Incident("aaaaaaaa", Severity.High); a.Resolve(TestData.T0.AddMinutes(30));
        var b = TestData.Incident("bbbbbbbb", Severity.High); b.Resolve(TestData.T0.AddMinutes(90));
        var c = TestData.Incident("cccccccc", Severity.High);                  // open
        var d = TestData.Incident("dddddddd", Severity.Low); d.Resolve(TestData.T0.AddMinutes(10));
        foreach (var i in new[] { a, b, c, d }) _repository.Seed(i);

        var report = await Handler.Handle(Period, CancellationToken.None);

        Assert.Equal(4, report.Total);
        Assert.Equal(1, report.Open);
        Assert.Equal(3, report.Resolved);
        Assert.Equal((30 + 90 + 10) / 3.0, report.MeanTimeToResolveMinutes);

        var high = report.BySeverity.Single(s => s.Severity == Severity.High);
        Assert.Equal(3, high.Total);
        Assert.Equal(1, high.Open);
        Assert.Equal(60, high.MeanTimeToResolveMinutes);
    }

    [Fact]
    public async Task Mean_time_to_resolve_is_null_when_nothing_is_resolved()
    {
        _repository.Seed(TestData.Incident());

        var report = await Handler.Handle(Period, CancellationToken.None);

        Assert.Null(report.MeanTimeToResolveMinutes);
        Assert.Equal(4, report.BySeverity.Count); // every severity is always reported
    }

    [Fact]
    public async Task Only_includes_incidents_created_in_the_period()
    {
        _repository.Seed(TestData.Incident("aaaaaaaa", createdAt: TestData.T0));
        _repository.Seed(TestData.Incident("bbbbbbbb", createdAt: TestData.T0.AddDays(-30)));

        var report = await Handler.Handle(Period, CancellationToken.None);

        Assert.Equal(1, report.Total);
    }

    [Fact]
    public async Task Rejects_reversed_or_excessive_ranges()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Handler.Handle(new GetAnalyticsQuery(TestData.T0, TestData.T0.AddDays(-1)), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Handler.Handle(new GetAnalyticsQuery(TestData.T0, TestData.T0.AddDays(400)), CancellationToken.None));
    }
}

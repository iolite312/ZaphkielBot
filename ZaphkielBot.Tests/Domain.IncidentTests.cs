using ZaphkielBotCore.Domain;
using ZaphkielBot.Tests.Fakes;

namespace ZaphkielBot.Tests;

public class IncidentTests
{
    [Theory]
    [InlineData("a1b2c3d4", "a1b2c3d4")]
    [InlineData("INC-A1B2C3D4", "a1b2c3d4")]
    [InlineData("  inc-a1b2c3d4 ", "a1b2c3d4")]
    [InlineData("a1b2c3", null)]
    [InlineData("zzzzzzzz", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Incident_ids_are_normalized(string? input, string? expected) =>
        Assert.Equal(expected, IncidentId.Normalize(input));

    [Fact]
    public void Report_enforces_title_and_description_rules()
    {
        Assert.NotNull(Incident.ValidateReport("ab", null));
        Assert.NotNull(Incident.ValidateReport(new string('x', 101), null));
        Assert.NotNull(Incident.ValidateReport("ok title", new string('x', 1501)));
        Assert.Null(Incident.ValidateReport("ok title", null));

        Assert.Throws<ArgumentException>(() =>
            Incident.Report("a1b2c3d4", "ab", null, Severity.Low, "u", "n", "c", TestData.T0));
    }

    [Fact]
    public void Resolve_never_produces_a_negative_time_to_resolve()
    {
        var incident = TestData.Incident();

        Assert.True(incident.Resolve(TestData.T0.AddMinutes(-5)));

        Assert.Equal(TimeSpan.Zero, incident.TimeToResolve);
    }

    [Fact]
    public void Resolve_only_transitions_once()
    {
        var incident = TestData.Incident();

        Assert.True(incident.Resolve(TestData.T0.AddMinutes(1)));
        Assert.False(incident.Resolve(TestData.T0.AddMinutes(9)));
        Assert.Equal(TimeSpan.FromMinutes(1), incident.TimeToResolve);
    }
}

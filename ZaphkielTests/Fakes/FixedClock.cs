namespace ZaphkielTests.Fakes;

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public FixedClock() : this(TestData.T0) { }

    public override DateTimeOffset GetUtcNow() => now;
}

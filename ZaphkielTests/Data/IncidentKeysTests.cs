using ZaphkielData.Entities;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Data;

public class IncidentKeysTests
{
    [Fact]
    public void Partition_is_the_creation_month()
    {
        Assert.Equal("202610", IncidentKeys.PartitionKeyFor(TestData.T0));
        Assert.Equal("202601", IncidentKeys.PartitionKeyFor(new DateTimeOffset(2026, 1, 31, 23, 59, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Newer_incidents_sort_first_within_a_partition()
    {
        var older = IncidentKeys.RowKeyFor(TestData.T0, "aaaaaaaa");
        var newer = IncidentKeys.RowKeyFor(TestData.T0.AddMinutes(1), "bbbbbbbb");

        Assert.True(string.CompareOrdinal(newer, older) < 0);
    }

    [Fact]
    public void Row_key_range_brackets_exactly_the_requested_period()
    {
        var from = TestData.T0;
        var to = TestData.T0.AddHours(2);
        var lower = IncidentKeys.RowKeyLowerBound(to);
        var upper = IncidentKeys.RowKeyUpperBound(from);

        bool InRange(string rowKey) => string.CompareOrdinal(rowKey, lower) >= 0 && string.CompareOrdinal(rowKey, upper) <= 0;

        Assert.True(InRange(IncidentKeys.RowKeyFor(from, "ffffffff")));      // start boundary, any id
        Assert.True(InRange(IncidentKeys.RowKeyFor(to, "00000000")));        // end boundary, any id
        Assert.True(InRange(IncidentKeys.RowKeyFor(from.AddHours(1), "abcdef12")));
        Assert.False(InRange(IncidentKeys.RowKeyFor(from.AddSeconds(-1), "abcdef12")));
        Assert.False(InRange(IncidentKeys.RowKeyFor(to.AddSeconds(1), "abcdef12")));
    }
}

using ZaphkielShared.Models;
using ZaphkielShared.Messaging;
using ZaphkielShared.Models;

namespace ZaphkielShared.Queries;

public record GetAnalyticsQuery(DateTimeOffset From, DateTimeOffset To) : IQuery<AnalyticsReport>
{
    public const int MaxRangeDays = 366;
}

public record SeverityStats(
    Severity Severity,
    int Total,
    int Open,
    int Resolved,
    double? MeanTimeToResolveMinutes);

public record AnalyticsReport(
    DateTimeOffset From,
    DateTimeOffset To,
    int Total,
    int Open,
    int Resolved,
    double? MeanTimeToResolveMinutes,
    IReadOnlyList<SeverityStats> BySeverity);

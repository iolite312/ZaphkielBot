using ZaphkielShared.Domain;
using ZaphkielShared.Messaging;

namespace ZaphkielShared.Queries;

public sealed record GetAnalyticsQuery(DateTimeOffset From, DateTimeOffset To) : IQuery<AnalyticsReport>
{
    public const int MaxRangeDays = 366;
}

public sealed record SeverityStats(
    Severity Severity,
    int Total,
    int Open,
    int Resolved,
    double? MeanTimeToResolveMinutes);

public sealed record AnalyticsReport(
    DateTimeOffset From,
    DateTimeOffset To,
    int Total,
    int Open,
    int Resolved,
    double? MeanTimeToResolveMinutes,
    IReadOnlyList<SeverityStats> BySeverity);

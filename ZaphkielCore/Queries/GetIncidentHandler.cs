using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielShared.Queries;

namespace ZaphkielCore.Queries;

public sealed class GetIncidentHandler(IIncidentReadRepository incidents) : IQueryHandler<GetIncidentQuery, IncidentView?>
{
    public async Task<IncidentView?> Handle(GetIncidentQuery query, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetByIdAsync(query.IncidentId, cancellationToken);
        return incident is null ? null : IncidentView.From(incident);
    }
}

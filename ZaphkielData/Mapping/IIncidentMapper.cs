using ZaphkielData.Entities;
using ZaphkielShared.Domain;

namespace ZaphkielData.Mapping;

/// <summary>Maps between the domain model and the table rows (including the secondary index rows).</summary>
public interface IIncidentMapper
{
    IncidentEntity ToEntity(Incident incident);
    Incident ToDomain(IncidentEntity entity);

    IncidentLookupEntity ToIdLookup(Incident incident);
    IncidentLookupEntity ToIssueLookup(Incident incident, int issueNumber);
}

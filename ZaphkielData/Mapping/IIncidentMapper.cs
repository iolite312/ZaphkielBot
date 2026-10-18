using ZaphkielData.Entities;
using ZaphkielShared.Domain;

namespace ZaphkielData.Mapping;

public interface IIncidentMapper
{
    IncidentEntity ToEntity(Incident incident);
    Incident ToDomain(IncidentEntity entity);

    IncidentLookupEntity ToIdLookup(Incident incident);
    IncidentLookupEntity ToIssueLookup(Incident incident, int issueNumber);
}

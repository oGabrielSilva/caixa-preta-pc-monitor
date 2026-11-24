using PcMonitor.Domain.Incidents;

namespace PcMonitor.Application.Incidents;

/// <summary>What incident detection writes. Each call is one transaction.</summary>
public interface IIncidentWriter
{
    /// <summary>
    /// Saves the incident, its related events and (when given) the closure of the session that went down, atomically.
    /// Returns <c>false</c>, writing nothing, when that shutdown is already saved.
    /// </summary>
    bool SaveIncident(Incident incident, SessionClosure? closure);

    /// <summary>Ends a session (and its open segment) that has no incident.</summary>
    void CloseSession(SessionClosure closure);
}

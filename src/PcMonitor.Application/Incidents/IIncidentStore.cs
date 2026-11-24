using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Incidents;

/// <summary>Durable storage for incidents and for closing orphaned sessions.</summary>
public interface IIncidentStore
{
    /// <summary><see cref="UnexpectedShutdown.LoggedAt"/> of the newest saved incident, or <c>null</c>.</summary>
    DateTimeOffset? LatestIncidentLoggedAt();

    /// <summary>Sessions without an end reason, oldest first.</summary>
    IReadOnlyList<OpenSession> OpenSessions();

    /// <summary>Saves the incident and its related events; <c>false</c> if that shutdown event was already saved.</summary>
    bool SaveIncident(Incident incident);

    /// <summary>Ends the session's open segment (if any) and the session, in one transaction.</summary>
    void EndSession(OpenSession session, DateTimeOffset at, EndReason reason);
}

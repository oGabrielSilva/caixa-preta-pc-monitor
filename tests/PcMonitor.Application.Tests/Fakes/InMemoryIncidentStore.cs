using PcMonitor.Application.Incidents;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Tests.Fakes;

/// <summary>Incident store in memory, keyed like the real one by (record id, logged at).</summary>
internal sealed class InMemoryIncidentStore : IIncidentStore
{
    public List<OpenSession> Open { get; } = [];

    public List<Incident> Incidents { get; } = [];

    public List<(OpenSession Session, DateTimeOffset At, EndReason Reason)> Ended { get; } = [];

    public DateTimeOffset? LatestIncidentLoggedAt() =>
        Incidents.Count == 0 ? null : Incidents.Max(i => i.Shutdown.LoggedAt);

    public IReadOnlyList<OpenSession> OpenSessions() => [.. Open.OrderBy(s => s.StartedAt)];

    public bool SaveIncident(Incident incident)
    {
        if (Incidents.Any(i => i.Shutdown.RecordId == incident.Shutdown.RecordId && i.Shutdown.LoggedAt == incident.Shutdown.LoggedAt))
        {
            return false;
        }

        Incidents.Add(incident);
        return true;
    }

    public void EndSession(OpenSession session, DateTimeOffset at, EndReason reason)
    {
        Ended.Add((session, at, reason));
        Open.Remove(session);
    }
}

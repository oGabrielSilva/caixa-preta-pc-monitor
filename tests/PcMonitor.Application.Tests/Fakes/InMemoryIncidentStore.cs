using PcMonitor.Application.Incidents;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Application.Tests.Fakes;

/// <summary>Incident storage in memory; can be told to fail for one shutdown record.</summary>
internal sealed class InMemoryIncidentStore : IIncidentReader, IIncidentWriter
{
    public List<OpenSession> Open { get; } = [];

    public List<(Incident Incident, SessionClosure? Closure)> Saved { get; } = [];

    public List<SessionClosure> Closed { get; } = [];

    public long? FailOnRecordId { get; set; }

    public IReadOnlyList<Incident> Incidents => [.. Saved.Select(s => s.Incident)];

    public IReadOnlySet<IncidentKey> SavedIncidents() => Saved.Select(s => IncidentKey.Of(s.Incident.Shutdown)).ToHashSet();

    public IReadOnlyList<OpenSession> OpenSessions() => [.. Open.OrderBy(s => s.StartedAt)];

    public void SaveIncident(Incident incident, SessionClosure? closure)
    {
        if (incident.Shutdown.RecordId == FailOnRecordId)
        {
            throw new IOException("database busy");
        }

        Saved.Add((incident, closure));
        if (closure is not null)
        {
            Open.Remove(closure.Session);
        }
    }

    public void CloseSession(SessionClosure closure)
    {
        Closed.Add(closure);
        Open.Remove(closure.Session);
    }
}

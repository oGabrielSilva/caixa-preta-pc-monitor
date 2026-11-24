namespace PcMonitor.Application.Incidents;

/// <summary>What incident detection needs to read from storage.</summary>
public interface IIncidentReader
{
    /// <summary>Keys of every incident already saved.</summary>
    IReadOnlySet<IncidentKey> SavedIncidents();

    /// <summary>Sessions without an end reason, oldest first.</summary>
    IReadOnlyList<OpenSession> OpenSessions();
}

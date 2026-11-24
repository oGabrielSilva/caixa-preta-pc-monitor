namespace PcMonitor.Application.Incidents;

/// <summary>Reads incidents and their diagnoses for display.</summary>
public interface IIncidentQuery
{
    /// <summary>Every incident, newest first.</summary>
    IReadOnlyList<IncidentSummary> List();

    /// <summary>One incident, or <c>null</c> when it does not exist.</summary>
    IncidentDetail? Find(long id);
}

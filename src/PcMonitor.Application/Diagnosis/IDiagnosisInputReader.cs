using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Application.Diagnosis;

/// <summary>Finds incidents that still need a diagnosis and builds each one's input.</summary>
public interface IDiagnosisInputReader
{
    /// <summary>Ids of incidents without a diagnosis, or with one made by an engine older than <paramref name="engineVersion"/>.</summary>
    IReadOnlyList<long> IncidentsNeedingDiagnosis(int engineVersion);

    /// <summary>Builds one incident's input (loaded one at a time so a bad row affects only that incident).</summary>
    IncidentEvidence Load(long incidentId);
}

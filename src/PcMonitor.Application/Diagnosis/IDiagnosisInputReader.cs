using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Application.Diagnosis;

/// <summary>Builds the inputs of incidents that still need a diagnosis.</summary>
public interface IDiagnosisInputReader
{
    /// <summary>Incidents without a diagnosis, or with one made by an engine older than <paramref name="engineVersion"/>.</summary>
    IReadOnlyList<IncidentEvidence> IncidentsNeedingDiagnosis(int engineVersion);
}

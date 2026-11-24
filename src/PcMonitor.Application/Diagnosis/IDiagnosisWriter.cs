using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Application.Diagnosis;

/// <summary>Stores diagnoses, replacing older ones.</summary>
public interface IDiagnosisWriter
{
    void Save(long incidentId, IncidentDiagnosis diagnosis);
}

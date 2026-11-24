using Microsoft.Extensions.Logging;
using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Application.Diagnosis;

/// <summary>
/// Diagnoses every incident without a diagnosis of the current engine version (new incidents, and all of them after
/// the rules change). A failure on one incident is logged and never stops the others.
/// </summary>
public sealed partial class DiagnoseIncidents(
    IDiagnosisInputReader reader, IDiagnosisWriter writer, DiagnosisEngine engine, ILogger<DiagnoseIncidents> logger)
{
    /// <returns>Number of incidents diagnosed and saved.</returns>
    public int Run()
    {
        var saved = 0;
        foreach (var evidence in reader.IncidentsNeedingDiagnosis(DiagnosisEngine.Version))
        {
            try
            {
                var diagnosis = engine.Diagnose(evidence);
                writer.Save(evidence.IncidentId, diagnosis);
                saved++;
                LogDiagnosed(logger, evidence.IncidentId, diagnosis.Top, diagnosis.Confidence);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                LogDiagnosisFailed(logger, exception, evidence.IncidentId);
            }
        }

        return saved;
    }

    [LoggerMessage(EventId = 300, Level = LogLevel.Information,
        Message = "Incident {IncidentId} diagnosed: most likely {Top} (confidence {Confidence}).")]
    private static partial void LogDiagnosed(ILogger logger, long incidentId, Hypothesis? top, Confidence confidence);

    [LoggerMessage(EventId = 301, Level = LogLevel.Error, Message = "Diagnosing incident {IncidentId} failed; it will be retried at the next start.")]
    private static partial void LogDiagnosisFailed(ILogger logger, Exception exception, long incidentId);
}

using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// The last-alive time equals the event log start of that boot: Windows went down before its first heartbeat after
/// booting (about 40 minutes). A neutral observation shown to the owner, not evidence for one cause.
/// </summary>
public sealed class BootTimingRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Incident.Shutdown.LastAliveAt is not { } lastAlive)
        {
            yield break;
        }

        if (evidence.Incident.RelatedEvents.Any(e => e.Provider == "EventLog" && e.EventId == 6005 && (e.LoggedAt - lastAlive).Duration() <= SameInstant))
        {
            yield return Finding.Observation("boot.early_failure");
        }
    }
}

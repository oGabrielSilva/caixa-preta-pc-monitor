using PcMonitor.Domain.Incidents;
using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// Time from the last frame to the next boot. Shown as an observation, not evidence: the event log cannot tell
/// "came back by itself" from "someone pressed the power button", and a weak supply can also restart by itself.
/// Only meaningful when the last frame is the failure time.
/// </summary>
public sealed class RestartGapRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var incident = evidence.Incident;
        if (incident.FailureTimeSource != FailureTimeSource.LastFrame || incident.TelemetryEndAt is not { } end)
        {
            yield break;
        }

        var gap = incident.Shutdown.LoggedAt - end;
        if (gap < TimeSpan.Zero)
        {
            yield break; // Clock adjusted between the last frame and the next boot: no meaningful gap.
        }

        if (gap <= QuickReturn)
        {
            yield return Finding.Observation(FindingCodes.RestartQuick, FindingParameters.Of(("seconds", (long)gap.TotalSeconds)));
        }
        else if (gap >= StayedOff)
        {
            yield return Finding.Observation(FindingCodes.RestartStayedOff, FindingParameters.Of(("minutes", (long)gap.TotalMinutes)));
        }
    }
}

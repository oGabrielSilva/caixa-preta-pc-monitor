using PcMonitor.Domain.Incidents;
using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// Time from the last frame to the next boot. A tripped supply protection keeps the machine off; coming back by itself
/// within minutes looks like a reset (board, reset line). Only meaningful when the last frame is the failure time.
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
        if (gap <= AutomaticRestart)
        {
            var parameters = FindingParameters.Of(("seconds", (long)gap.TotalSeconds));
            yield return Finding.For("restart.automatic", Hypothesis.BoardPowerDelivery, EvidenceStrength.Moderate, parameters);
            yield return Finding.Against("restart.automatic", Hypothesis.PowerSupply, EvidenceStrength.Weak, parameters);
        }
        else if (gap >= StayedOff)
        {
            yield return Finding.For("restart.stayed_off", Hypothesis.PowerSupply, EvidenceStrength.Weak,
                FindingParameters.Of(("minutes", (long)gap.TotalMinutes)));
        }
    }
}

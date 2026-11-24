namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// A crash while entering or leaving sleep/hibernate (including Fast Startup): power transitions stress the board's
/// power delivery and the supply's response.
/// </summary>
public sealed class SleepTransitionRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var state = evidence.Incident.Shutdown.SleepState;
        if (state == 0)
        {
            yield break;
        }

        var parameters = FindingParameters.Of(("state", state));
        yield return Finding.For("power.transition", Hypothesis.BoardPowerDelivery, EvidenceStrength.Moderate, parameters);
        yield return Finding.For("power.transition", Hypothesis.PowerSupply, EvidenceStrength.Weak, parameters);
    }
}

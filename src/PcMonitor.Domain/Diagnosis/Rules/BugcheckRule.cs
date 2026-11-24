using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// Blue screen recorded with the shutdown. Its absence is weak evidence only: a hard power cut, a reset or a thermal
/// trip also leave no blue screen.
/// </summary>
public sealed class BugcheckRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var code = evidence.Incident.Shutdown.BugcheckCode;
        var parameters = FindingParameters.Of(("code", $"0x{code:X8}"));

        if (code == WheaBugcheck)
        {
            yield break; // A hardware error, not software: weighed by WheaRule together with its other traces.
        }

        yield return code == 0
            ? Finding.Against("bugcheck.none", Hypothesis.Software, EvidenceStrength.Weak)
            : Finding.For("bugcheck", Hypothesis.Software, EvidenceStrength.Strong, parameters);
    }
}

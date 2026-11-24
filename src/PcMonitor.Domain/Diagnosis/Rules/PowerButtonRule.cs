namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// A held power button forces the machine off and Windows logs it like a crash. A plain recorded press is weak: it
/// may be any press during the session.
/// </summary>
public sealed class PowerButtonRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var shutdown = evidence.Incident.Shutdown;
        if (shutdown.LongPowerButtonPress)
        {
            yield return Finding.For(FindingCodes.PowerButtonLongPress, Hypothesis.ForcedPowerOff, EvidenceStrength.Strong);
        }
        else if (shutdown.PowerButtonPressedAt is not null)
        {
            yield return Finding.For(FindingCodes.PowerButtonPressed, Hypothesis.ForcedPowerOff, EvidenceStrength.Weak);
        }
    }
}

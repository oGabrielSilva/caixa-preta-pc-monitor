using PcMonitor.Domain.Sensors;
using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// CPU load in the last seconds. Supplies and coolers usually give out under load; dying while idle leans weakly
/// towards the board (reset line, short, VRM), without ruling out the supply.
/// </summary>
public sealed class LoadAtFailureRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.AtFailure is not { } telemetry)
        {
            yield break;
        }

        if (!telemetry.Has(SensorRole.CpuLoad))
        {
            yield return SensorLimitation.Unavailable(SensorRole.CpuLoad);
            yield break;
        }

        var end = telemetry.Tail(SensorRole.CpuLoad, End);
        if (end.Count < MinFramesAtEnd)
        {
            yield return SensorLimitation.TooShort(SensorRole.CpuLoad);
            yield break;
        }

        var average = end.Average(p => p.Value);
        var parameters = FindingParameters.Of(("avg", FindingParameters.Decimal(average)));
        if (average >= HighLoadPercent)
        {
            yield return Finding.For("load.high", Hypothesis.PowerSupply, EvidenceStrength.Weak, parameters);
            yield return Finding.For("load.high", Hypothesis.Thermal, EvidenceStrength.Weak, parameters);
        }
        else if (average <= IdleLoadPercent)
        {
            yield return Finding.For("load.idle", Hypothesis.BoardPowerDelivery, EvidenceStrength.Weak, parameters);
        }
    }
}

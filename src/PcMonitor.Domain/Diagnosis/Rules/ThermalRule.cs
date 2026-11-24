using PcMonitor.Domain.Sensors;
using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>CPU temperature before the failure: a thermal trip shuts the machine down at around 95 °C on this CPU.</summary>
public sealed class ThermalRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Telemetry is not { } telemetry)
        {
            yield break;
        }

        if (!telemetry.Has(SensorRole.CpuTemperature))
        {
            yield return SensorLimitation.Unavailable(SensorRole.CpuTemperature);
            yield break;
        }

        var max = telemetry.Series(SensorRole.CpuTemperature).Max(p => p.Value);
        var parameters = FindingParameters.Of(("max", FindingParameters.Decimal(max)));
        if (max >= CpuCriticalCelsius)
        {
            yield return Finding.For("thermal.critical", Hypothesis.Thermal, EvidenceStrength.Strong, parameters);
        }
        else if (max >= CpuHighCelsius)
        {
            yield return Finding.For("thermal.high", Hypothesis.Thermal, EvidenceStrength.Moderate, parameters);
        }
        else if (max < CpuNormalBelowCelsius)
        {
            yield return Finding.Against("thermal.normal", Hypothesis.Thermal, EvidenceStrength.Moderate, parameters);
        }

        var recent = telemetry.Tail(SensorRole.CpuTemperature, CpuRiseSpan);
        if (recent.Count >= MinFramesForTrend && recent[^1].Value - recent[0].Value >= CpuRiseCelsius)
        {
            yield return Finding.For("thermal.rising", Hypothesis.Thermal, EvidenceStrength.Weak,
                FindingParameters.Of(("delta", FindingParameters.Decimal(recent[^1].Value - recent[0].Value))));
        }
    }
}

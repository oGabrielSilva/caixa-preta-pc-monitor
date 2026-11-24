using PcMonitor.Domain.Sensors;
using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// CPU temperature at the failure (the last seconds, never an earlier peak). The Ryzen 7 8700G runs at its 95 °C
/// limit under sustained load by design, so only readings above it, or sitting at it when the machine died, count.
/// </summary>
public sealed class ThermalRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.AtFailure is not { } telemetry)
        {
            yield break;
        }

        if (!telemetry.Has(SensorRole.CpuTemperature))
        {
            yield return SensorLimitation.Unavailable(SensorRole.CpuTemperature);
            yield break;
        }

        var end = telemetry.Tail(SensorRole.CpuTemperature, End);
        if (end.Count < MinFramesAtEnd)
        {
            yield return SensorLimitation.TooShort(SensorRole.CpuTemperature);
            yield break;
        }

        var max = end.Max(p => p.Value);
        var average = end.Average(p => p.Value);
        if (max >= CpuCriticalCelsius)
        {
            yield return Finding.For(FindingCodes.ThermalCritical, Hypothesis.Thermal, EvidenceStrength.Strong,
                FindingParameters.Of(("max", FindingParameters.Decimal(max))));
        }
        else if (average >= CpuAtLimitCelsius)
        {
            yield return Finding.For(FindingCodes.ThermalAtLimit, Hypothesis.Thermal, EvidenceStrength.Moderate,
                FindingParameters.Of(("avg", FindingParameters.Decimal(average))));
        }
        else if (max < CpuNormalBelowCelsius)
        {
            yield return Finding.Against(FindingCodes.ThermalNormal, Hypothesis.Thermal, EvidenceStrength.Moderate,
                FindingParameters.Of(("max", FindingParameters.Decimal(max))));
        }
    }
}

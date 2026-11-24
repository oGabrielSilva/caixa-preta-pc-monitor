using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// A rail sagging in the last seconds while still inside its ATX range (out-of-range readings belong to
/// <see cref="RailToleranceRule"/>, so one sample is never counted twice). Weighs supply and board equally: the
/// sensor sits on the board.
/// </summary>
public sealed class RailDroopRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.AtFailure is not { } telemetry)
        {
            yield break;
        }

        foreach (var rail in RailTolerance.Atx.Where(r => r.DroopVolts is not null && telemetry.Has(r.Role)))
        {
            var series = telemetry.Series(rail.Role);
            var end = telemetry.Tail(rail.Role, End);
            if (series.Count < MinFramesForTrend || end.Count == 0)
            {
                yield return SensorLimitation.TooShort(rail.Role);
                continue;
            }

            var median = Median(series.Select(p => p.Value));
            var lowest = end.Min(p => p.Value);
            if (lowest <= median - rail.DroopVolts!.Value && lowest >= rail.Min)
            {
                var parameters = FindingParameters.Of(
                    ("rail", rail.Name), ("median", FindingParameters.Volts(median)), ("lowest", FindingParameters.Volts(lowest)));
                yield return Finding.For(FindingCodes.RailDroop, Hypothesis.PowerSupply, EvidenceStrength.Weak, parameters);
                yield return Finding.For(FindingCodes.RailDroop, Hypothesis.BoardPowerDelivery, EvidenceStrength.Weak, parameters);
            }
        }
    }

    private static float Median(IEnumerable<float> values)
    {
        var sorted = values.Order().ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }
}

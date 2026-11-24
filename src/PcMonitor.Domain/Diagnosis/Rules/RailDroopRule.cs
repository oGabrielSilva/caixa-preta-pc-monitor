using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>A rail sagging in the last seconds compared with the rest of the window.</summary>
public sealed class RailDroopRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Telemetry is not { } telemetry)
        {
            yield break;
        }

        foreach (var rail in RailTolerance.Atx.Where(r => r.DroopVolts is not null))
        {
            var series = telemetry.Series(rail.Role);
            var end = telemetry.Tail(rail.Role, End);
            if (series.Count < MinFramesForTrend || end.Count == 0)
            {
                continue;
            }

            var median = Median(series.Select(p => p.Value));
            var min = end.Min(p => p.Value);
            if (min <= median - rail.DroopVolts!.Value)
            {
                yield return Finding.For("rail.droop", Hypothesis.PowerSupply, EvidenceStrength.Moderate, FindingParameters.Of(
                    ("rail", rail.Name), ("median", FindingParameters.Volts(median)), ("min", FindingParameters.Volts(min))));
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

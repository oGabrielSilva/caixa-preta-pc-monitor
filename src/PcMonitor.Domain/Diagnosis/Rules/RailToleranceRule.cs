using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// Supply rails outside their ATX range before the failure. The board's sensor chip measures after the 24-pin
/// connector and the board traces, so a bad reading weighs the supply and the board equally. Counted once per incident
/// (the worst rail), and only when at least two samples are out of range.
/// </summary>
public sealed class RailToleranceRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.AtFailure is not { } telemetry)
        {
            yield break;
        }

        var measured = new List<(RailTolerance Rail, IReadOnlyList<(DateTimeOffset At, float Value)> Series)>();
        foreach (var rail in RailTolerance.Atx)
        {
            if (telemetry.Has(rail.Role))
            {
                measured.Add((rail, telemetry.Series(rail.Role)));
            }
            else
            {
                yield return SensorLimitation.Unavailable(rail.Role);
            }
        }

        if (measured.Count == 0)
        {
            yield break;
        }

        var worst = measured
            .Select(m => (m.Rail, Out: m.Series.Where(p => !m.Rail.Contains(p.Value)).Select(p => p.Value).ToList()))
            .Where(m => m.Out.Count >= MinSamplesOutOfTolerance)
            .OrderByDescending(m => m.Out.Max(m.Rail.Excess))
            .FirstOrDefault();

        if (worst.Rail is null)
        {
            yield return Finding.Observation("rail.in_tolerance", FindingParameters.Of(("rails", string.Join(',', measured.Select(m => m.Rail.Name)))));
            yield return Finding.Limitation("rail.precision");
            yield break;
        }

        var parameters = FindingParameters.Of(
            ("rail", worst.Rail.Name), ("min", FindingParameters.Volts(worst.Rail.Min)), ("max", FindingParameters.Volts(worst.Rail.Max)),
            ("observed", FindingParameters.Volts(worst.Out.MaxBy(worst.Rail.Excess))), ("samples", worst.Out.Count));
        yield return Finding.For("rail.out_of_tolerance", Hypothesis.PowerSupply, EvidenceStrength.Moderate, parameters);
        yield return Finding.For("rail.out_of_tolerance", Hypothesis.BoardPowerDelivery, EvidenceStrength.Moderate, parameters);
        yield return Finding.Limitation("rail.measured_on_board");
    }
}

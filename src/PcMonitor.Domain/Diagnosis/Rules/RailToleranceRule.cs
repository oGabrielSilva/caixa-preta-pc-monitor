namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>Any supply rail outside its ATX range before the failure.</summary>
public sealed class RailToleranceRule : IDiagnosticRule
{
    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Telemetry is not { } telemetry)
        {
            yield break;
        }

        var checkedRails = new List<string>();
        var anyOut = false;
        foreach (var rail in RailTolerance.Atx)
        {
            if (!telemetry.Has(rail.Role))
            {
                yield return SensorLimitation.Unavailable(rail.Role);
                continue;
            }

            checkedRails.Add(rail.Name);
            var worst = telemetry.Series(rail.Role).MaxBy(p => rail.Excess(p.Value)).Value;
            if (!rail.Contains(worst))
            {
                anyOut = true;
                yield return Finding.For("rail.out_of_tolerance", Hypothesis.PowerSupply, EvidenceStrength.Strong, FindingParameters.Of(
                    ("rail", rail.Name), ("min", FindingParameters.Volts(rail.Min)), ("max", FindingParameters.Volts(rail.Max)),
                    ("observed", FindingParameters.Volts(worst))));
            }
        }

        if (!anyOut && checkedRails.Count > 0)
        {
            yield return Finding.Against("rail.in_tolerance", Hypothesis.PowerSupply, EvidenceStrength.Weak,
                FindingParameters.Of(("rails", string.Join(',', checkedRails))));
            yield return Finding.Limitation("rail.precision");
        }
    }
}

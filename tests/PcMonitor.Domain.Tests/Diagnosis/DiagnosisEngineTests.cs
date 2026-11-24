using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis;

public sealed class DiagnosisEngineTests
{
    private sealed class FixedRule(params Finding[] findings) : IDiagnosticRule
    {
        public IEnumerable<Finding> Evaluate(IncidentEvidence evidence) => findings;
    }

    private static IncidentDiagnosis Diagnose(IncidentEvidence evidence, params Finding[] findings) =>
        new DiagnosisEngine([new FixedRule(findings)]).Diagnose(evidence);

    private static IncidentEvidence WithTelemetry() => new EvidenceBuilder().WithTelemetry().Build();

    [Fact]
    public void Engine_sums_weights_per_hypothesis_and_ranks_descending_breaking_ties_by_enum_order()
    {
        var diagnosis = Diagnose(WithTelemetry(),
            Finding.For("a", Hypothesis.Thermal, EvidenceStrength.Moderate),
            Finding.For("b", Hypothesis.PowerSupply, EvidenceStrength.Strong),
            Finding.Against("c", Hypothesis.PowerSupply, EvidenceStrength.Weak));

        diagnosis.Ranking.ShouldBe([new HypothesisScore(Hypothesis.PowerSupply, 2), new HypothesisScore(Hypothesis.Thermal, 2)]);
        diagnosis.EngineVersion.ShouldBe(DiagnosisEngine.Version);
        diagnosis.Findings.Count.ShouldBe(3);
    }

    [Fact]
    public void Observations_do_not_score()
    {
        var diagnosis = Diagnose(WithTelemetry(), Finding.Observation("boot.early_failure"));

        diagnosis.Ranking.ShouldBeEmpty();
        diagnosis.Findings.ShouldHaveSingleItem().Kind.ShouldBe(FindingKind.Observation);
    }

    [Theory]
    [InlineData(6, 2, Confidence.High)]
    [InlineData(5, 3, Confidence.Medium)]
    [InlineData(3, 0, Confidence.Medium)]
    [InlineData(1, 0, Confidence.Low)]
    [InlineData(0, 0, Confidence.Inconclusive)]
    [InlineData(0, -2, Confidence.Inconclusive)]
    public void Confidence_follows_score_and_margin(int top, int second, Confidence expected)
    {
        static IEnumerable<Finding> Points(Hypothesis hypothesis, int score) =>
            Enumerable.Repeat(score >= 0 ? Finding.For("x", hypothesis, EvidenceStrength.Weak) : Finding.Against("x", hypothesis, EvidenceStrength.Weak), Math.Abs(score));

        var diagnosis = Diagnose(WithTelemetry(), [.. Points(Hypothesis.PowerSupply, top), .. Points(Hypothesis.Thermal, second)]);

        diagnosis.Confidence.ShouldBe(expected);
    }

    [Fact]
    public void Limitations_are_kept_apart_and_deduplicated()
    {
        var diagnosis = Diagnose(WithTelemetry(),
            Finding.Limitation("rail.precision"), Finding.Limitation("rail.precision"), Finding.For("a", Hypothesis.Thermal, EvidenceStrength.Weak));

        diagnosis.Limitations.ShouldHaveSingleItem().Code.ShouldBe("rail.precision");
        diagnosis.Findings.ShouldHaveSingleItem().Code.ShouldBe("a");
    }

    [Fact]
    public void Missing_telemetry_is_always_a_limitation() =>
        Diagnose(new EvidenceBuilder { Source = PcMonitor.Domain.Incidents.FailureTimeSource.Unknown }.Build())
            .Limitations.Select(l => l.Code).ShouldContain("telemetry.missing");

    [Theory]
    [InlineData(PcMonitor.Domain.Incidents.FailureTimeSource.LastAliveEventLog)]
    [InlineData(PcMonitor.Domain.Incidents.FailureTimeSource.LastFrameBeforeSleep)]
    public void Lower_bound_failure_time_is_a_limitation(PcMonitor.Domain.Incidents.FailureTimeSource source) =>
        Diagnose(new EvidenceBuilder { Source = source, LastAliveAt = EvidenceBuilder.End }.WithTelemetry().Build())
            .Limitations.Select(l => l.Code).ShouldContain("estimate.lower_bound");
}

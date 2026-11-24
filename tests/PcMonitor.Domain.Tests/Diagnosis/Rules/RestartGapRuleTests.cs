using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class RestartGapRuleTests
{
    private static List<Finding> Evaluate(EvidenceBuilder builder) => [.. new RestartGapRule().Evaluate(builder.Build())];

    [Fact]
    public void Coming_back_within_three_minutes_means_it_restarted_by_itself()
    {
        var findings = Evaluate(new EvidenceBuilder { LoggedAt = EvidenceBuilder.End.AddSeconds(95) }.WithTelemetry());

        findings.Summary().ShouldBe(
        [
            RuleAssertions.For("restart.automatic", Hypothesis.BoardPowerDelivery, EvidenceStrength.Moderate),
            RuleAssertions.Against("restart.automatic", Hypothesis.PowerSupply, EvidenceStrength.Weak),
        ]);
        findings[0].Parameters["seconds"].ShouldBe("95");
    }

    [Fact]
    public void Staying_off_for_fifteen_minutes_or_more_is_weak_evidence_for_the_power_supply()
    {
        var finding = Evaluate(new EvidenceBuilder { LoggedAt = EvidenceBuilder.End.AddMinutes(40) }.WithTelemetry()).ShouldHaveSingleItem();

        finding.Summary().ShouldBe([RuleAssertions.For("restart.stayed_off", Hypothesis.PowerSupply, EvidenceStrength.Weak)]);
        finding.Parameters["minutes"].ShouldBe("40");
    }

    [Fact]
    public void A_gap_between_three_and_fifteen_minutes_says_nothing() =>
        Evaluate(new EvidenceBuilder { LoggedAt = EvidenceBuilder.End.AddMinutes(8) }.WithTelemetry()).ShouldBeEmpty();

    [Theory]
    [InlineData(FailureTimeSource.LastAliveEventLog)]
    [InlineData(FailureTimeSource.LastFrameBeforeSleep)]
    [InlineData(FailureTimeSource.Unknown)]
    public void Restart_gap_rule_ignores_lower_bound_or_unknown_failure_times(FailureTimeSource source) =>
        Evaluate(new EvidenceBuilder { Source = source, LastAliveAt = EvidenceBuilder.End, LoggedAt = EvidenceBuilder.End.AddSeconds(30) }.WithTelemetry())
            .ShouldBeEmpty();
}

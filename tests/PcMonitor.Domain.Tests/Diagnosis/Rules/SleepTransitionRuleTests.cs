using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class SleepTransitionRuleTests
{
    [Fact]
    public void A_crash_during_a_power_transition_points_to_power_delivery()
    {
        var findings = new SleepTransitionRule().Evaluate(new EvidenceBuilder { SleepState = 4 }.Build()).ToList();

        findings.Summary().ShouldBe(
        [
            RuleAssertions.For("power.transition", Hypothesis.BoardPowerDelivery, EvidenceStrength.Moderate),
            RuleAssertions.For("power.transition", Hypothesis.PowerSupply, EvidenceStrength.Weak),
        ]);
        findings[0].Parameters["state"].ShouldBe("4");
    }

    [Fact]
    public void No_transition_says_nothing() => new SleepTransitionRule().Evaluate(new EvidenceBuilder().Build()).ShouldBeEmpty();
}

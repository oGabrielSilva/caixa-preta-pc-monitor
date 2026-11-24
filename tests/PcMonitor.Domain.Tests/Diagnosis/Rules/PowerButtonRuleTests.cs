using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class PowerButtonRuleTests
{
    [Fact]
    public void A_long_press_is_strong_evidence_of_a_forced_power_off() =>
        new PowerButtonRule().Evaluate(new EvidenceBuilder { LongPowerButtonPress = true }.Build()).Summary()
            .ShouldBe([RuleAssertions.For("power_button.long_press", Hypothesis.ForcedPowerOff, EvidenceStrength.Strong)]);

    [Fact]
    public void A_recorded_press_is_weak_evidence() =>
        new PowerButtonRule().Evaluate(new EvidenceBuilder { PowerButtonPressedAt = EvidenceBuilder.End }.Build()).Summary()
            .ShouldBe([RuleAssertions.For("power_button.pressed", Hypothesis.ForcedPowerOff, EvidenceStrength.Weak)]);

    [Fact]
    public void No_press_says_nothing() => new PowerButtonRule().Evaluate(new EvidenceBuilder().Build()).ShouldBeEmpty();
}

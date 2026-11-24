using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Sensors;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class RailDroopRuleTests
{
    private static List<Finding> Evaluate(EvidenceBuilder builder) => [.. new RailDroopRule().Evaluate(builder.Build())];

    [Fact]
    public void A_12V_sag_at_the_end_is_moderate_evidence_for_the_power_supply()
    {
        float[] values = [.. Enumerable.Repeat(12.19f, 110), .. Enumerable.Repeat(11.95f, 10)];

        var finding = Evaluate(new EvidenceBuilder().Series(SensorRole.Rail12V, values)).ShouldHaveSingleItem();

        finding.Summary().ShouldBe([RuleAssertions.For("rail.droop", Hypothesis.PowerSupply, EvidenceStrength.Moderate)]);
        (finding.Parameters["rail"], finding.Parameters["median"], finding.Parameters["min"]).ShouldBe(("12V", "12.19", "11.95"));
    }

    [Fact]
    public void A_sag_smaller_than_the_threshold_is_ignored()
    {
        float[] values = [.. Enumerable.Repeat(12.19f, 110), .. Enumerable.Repeat(12.096f, 10)]; // one ADC step

        Evaluate(new EvidenceBuilder().Series(SensorRole.Rail12V, values)).ShouldBeEmpty();
    }

    [Fact]
    public void A_5V_sag_uses_its_own_threshold()
    {
        float[] values = [.. Enumerable.Repeat(5f, 110), .. Enumerable.Repeat(4.88f, 10)];

        Evaluate(new EvidenceBuilder().Series(SensorRole.Rail5V, values)).ShouldHaveSingleItem().Parameters["rail"].ShouldBe("5V");
    }

    [Fact]
    public void Droop_rule_does_not_conclude_with_fewer_than_ten_frames() =>
        Evaluate(new EvidenceBuilder().Series(SensorRole.Rail12V, 12.19f, 12.19f, 11.5f)).ShouldBeEmpty();
}

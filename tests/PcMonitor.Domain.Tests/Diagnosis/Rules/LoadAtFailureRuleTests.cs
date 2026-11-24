using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Sensors;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class LoadAtFailureRuleTests
{
    private static List<Finding> Evaluate(EvidenceBuilder builder) => [.. new LoadAtFailureRule().Evaluate(builder.Build())];

    [Fact]
    public void High_load_at_the_end_is_weak_evidence_for_power_supply_and_thermal() =>
        Evaluate(new EvidenceBuilder().Constant(SensorRole.CpuLoad, 90f, 20)).Summary().ShouldBe(
        [
            RuleAssertions.For("load.high", Hypothesis.PowerSupply, EvidenceStrength.Weak),
            RuleAssertions.For("load.high", Hypothesis.Thermal, EvidenceStrength.Weak),
        ]);

    [Fact]
    public void Idle_at_the_end_leans_weakly_to_the_board_without_ruling_out_the_supply() =>
        Evaluate(new EvidenceBuilder().Constant(SensorRole.CpuLoad, 5f, 20)).Summary()
            .ShouldBe([RuleAssertions.For("load.idle", Hypothesis.BoardPowerDelivery, EvidenceStrength.Weak)]);

    [Fact]
    public void Medium_load_says_nothing() => Evaluate(new EvidenceBuilder().Constant(SensorRole.CpuLoad, 45f, 20)).ShouldBeEmpty();

    [Fact]
    public void Too_few_recent_frames_is_a_limitation() =>
        Evaluate(new EvidenceBuilder().Constant(SensorRole.CpuLoad, 90f, 3)).Summary().ShouldBe([RuleAssertions.Limitation("telemetry.too_short")]);

    [Fact]
    public void Missing_load_sensor_is_a_limitation() =>
        Evaluate(new EvidenceBuilder().Constant(SensorRole.Rail12V, 12f)).Summary().ShouldBe([RuleAssertions.Limitation("sensor.unavailable")]);
}

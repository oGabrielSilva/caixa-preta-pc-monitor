using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Sensors;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class ThermalRuleTests
{
    private static List<Finding> Evaluate(EvidenceBuilder builder) => [.. new ThermalRule().Evaluate(builder.Build())];

    [Theory]
    [InlineData(102f, "thermal.critical", true, EvidenceStrength.Strong, "max")]
    [InlineData(96f, "thermal.at_limit", true, EvidenceStrength.Moderate, "avg")]
    [InlineData(60f, "thermal.normal", false, EvidenceStrength.Moderate, "max")]
    public void Temperature_at_the_failure_sets_the_thermal_evidence(
        float atEnd, string code, bool supports, EvidenceStrength strength, string parameter)
    {
        var finding = Evaluate(new EvidenceBuilder().Constant(SensorRole.CpuTemperature, atEnd, 20)).ShouldHaveSingleItem();

        (finding.Code, finding.Hypothesis, finding.Supports, finding.Strength).ShouldBe((code, Hypothesis.Thermal, supports, strength));
        finding.Parameters[parameter].ShouldBe(atEnd.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(80f)]
    [InlineData(93f)]
    public void Normal_load_temperatures_below_the_limit_say_nothing(float atEnd) =>
        Evaluate(new EvidenceBuilder().Constant(SensorRole.CpuTemperature, atEnd, 20)).ShouldBeEmpty();

    [Fact]
    public void Only_the_last_seconds_count_not_an_earlier_peak()
    {
        float[] values = [.. Enumerable.Repeat(102f, 60), .. Enumerable.Repeat(50f, 60)];

        Evaluate(new EvidenceBuilder().Series(SensorRole.CpuTemperature, values)).ShouldHaveSingleItem().Code.ShouldBe("thermal.normal");
    }

    [Fact]
    public void Too_few_frames_at_the_end_is_a_limitation() =>
        Evaluate(new EvidenceBuilder().Series(SensorRole.CpuTemperature, 99f, 99f)).Summary()
            .ShouldBe([RuleAssertions.Limitation("telemetry.too_short")]);

    [Fact]
    public void Thermal_rule_reports_the_missing_sensor_as_a_limitation()
    {
        var finding = Evaluate(new EvidenceBuilder().Constant(SensorRole.CpuLoad, 10f)).ShouldHaveSingleItem();

        finding.Summary().ShouldBe([RuleAssertions.Limitation("sensor.unavailable")]);
        finding.Parameters["role"].ShouldBe("CpuTemperature");
    }

    [Theory]
    [InlineData(FailureTimeSource.LastFrameBeforeSleep)]
    [InlineData(FailureTimeSource.LastAliveEventLog)]
    public void Telemetry_that_does_not_end_at_the_failure_says_nothing(FailureTimeSource source) =>
        Evaluate(new EvidenceBuilder { Source = source, LastAliveAt = EvidenceBuilder.End }.Constant(SensorRole.CpuTemperature, 102f, 20))
            .ShouldBeEmpty();
}

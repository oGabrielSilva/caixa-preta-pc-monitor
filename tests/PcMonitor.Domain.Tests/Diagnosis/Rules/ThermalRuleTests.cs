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
    [InlineData(92f, "thermal.critical", true, EvidenceStrength.Strong)]
    [InlineData(84f, "thermal.high", true, EvidenceStrength.Moderate)]
    [InlineData(60f, "thermal.normal", false, EvidenceStrength.Moderate)]
    public void Peak_cpu_temperature_sets_the_thermal_evidence(float peak, string code, bool supports, EvidenceStrength strength)
    {
        var finding = Evaluate(new EvidenceBuilder().Series(SensorRole.CpuTemperature, 50f, peak, 50f)).ShouldHaveSingleItem();

        (finding.Code, finding.Hypothesis, finding.Supports, finding.Strength).ShouldBe((code, Hypothesis.Thermal, supports, strength));
        finding.Parameters["max"].ShouldBe(peak.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Peak_between_75_and_80_says_nothing() =>
        Evaluate(new EvidenceBuilder().Constant(SensorRole.CpuTemperature, 77f, 5)).ShouldBeEmpty();

    [Fact]
    public void A_fast_rise_before_the_end_is_weak_evidence()
    {
        var rising = Enumerable.Range(0, 30).Select(i => 50f + i).ToArray(); // +29 °C over 30 s, peak 79

        Evaluate(new EvidenceBuilder().Series(SensorRole.CpuTemperature, rising)).Summary()
            .ShouldBe([RuleAssertions.For("thermal.rising", Hypothesis.Thermal, EvidenceStrength.Weak)]);
    }

    [Fact]
    public void Thermal_rule_reports_the_missing_sensor_as_a_limitation()
    {
        var finding = Evaluate(new EvidenceBuilder().Constant(SensorRole.CpuLoad, 10f)).ShouldHaveSingleItem();

        finding.Summary().ShouldBe([RuleAssertions.Limitation("sensor.unavailable")]);
        finding.Parameters["role"].ShouldBe("CpuTemperature");
    }

    [Fact]
    public void Without_telemetry_the_rule_says_nothing() =>
        Evaluate(new EvidenceBuilder { Source = FailureTimeSource.Unknown }).ShouldBeEmpty();
}

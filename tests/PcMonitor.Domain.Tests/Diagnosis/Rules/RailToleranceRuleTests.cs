using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Sensors;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class RailToleranceRuleTests
{
    private static readonly Dictionary<SensorRole, float> _nominal = new()
    {
        [SensorRole.Rail12V] = 12.19f,
        [SensorRole.Rail5V] = 5f,
        [SensorRole.Rail3V3] = 3.3f,
    };

    private static List<Finding> Evaluate(EvidenceBuilder builder) => [.. new RailToleranceRule().Evaluate(builder.Build())];

    private static EvidenceBuilder AllRails()
    {
        var builder = new EvidenceBuilder();
        foreach (var (role, volts) in _nominal)
        {
            builder.Constant(role, volts, 5);
        }

        return builder;
    }

    [Theory]
    [InlineData(SensorRole.Rail12V, 11.3f, "12V", "11.4", "12.6")]
    [InlineData(SensorRole.Rail5V, 5.3f, "5V", "4.75", "5.25")]
    [InlineData(SensorRole.Rail3V3, 3.1f, "3.3V", "3.135", "3.465")]
    public void A_rail_out_of_range_twice_weighs_supply_and_board_equally(SensorRole role, float bad, string rail, string min, string max)
    {
        var findings = Evaluate(AllRails().Series(role, _nominal[role], bad, bad));

        findings.Summary().ShouldBe(
        [
            RuleAssertions.For("rail.out_of_tolerance", Hypothesis.PowerSupply, EvidenceStrength.Moderate),
            RuleAssertions.For("rail.out_of_tolerance", Hypothesis.BoardPowerDelivery, EvidenceStrength.Moderate),
            RuleAssertions.Limitation("rail.measured_on_board"),
        ]);
        findings[0].Parameters.ShouldBe(new Dictionary<string, string>
        {
            ["rail"] = rail,
            ["min"] = min,
            ["max"] = max,
            ["observed"] = bad.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
            ["samples"] = "2",
        }, ignoreOrder: true);
    }

    [Fact]
    public void A_single_out_of_range_sample_is_treated_as_a_glitch() =>
        Evaluate(AllRails().Series(SensorRole.Rail12V, 12.19f, 11.3f, 12.19f)).Select(f => f.Code)
            .ShouldBe(["rail.in_tolerance", "rail.precision"]);

    [Fact]
    public void Several_bad_rails_are_counted_once_by_the_worst()
    {
        var findings = Evaluate(AllRails().Series(SensorRole.Rail12V, 11.3f, 11.3f).Series(SensorRole.Rail5V, 4.5f, 4.5f));

        findings.Count(f => f.Kind == FindingKind.Evidence).ShouldBe(2);
        findings[0].Parameters["rail"].ShouldBe("5V"); // 0.25 V outside beats 0.1 V
    }

    [Fact]
    public void Rails_within_range_are_an_observation_with_the_precision_limitation() =>
        Evaluate(AllRails()).Summary().ShouldBe(
        [
            RuleAssertions.Observation("rail.in_tolerance"),
            RuleAssertions.Limitation("rail.precision"),
        ]);

    [Fact]
    public void Missing_rails_are_limitations() =>
        Evaluate(new EvidenceBuilder().Constant(SensorRole.Rail3V3, 3.3f, 5))
            .Where(f => f.Code == "sensor.unavailable").Select(f => f.Parameters["role"]).ShouldBe(["Rail12V", "Rail5V"]);
}

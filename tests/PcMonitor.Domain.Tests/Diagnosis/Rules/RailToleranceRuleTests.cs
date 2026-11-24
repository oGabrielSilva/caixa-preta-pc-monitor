using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Sensors;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class RailToleranceRuleTests
{
    private static List<Finding> Evaluate(EvidenceBuilder builder) => [.. new RailToleranceRule().Evaluate(builder.Build())];

    private static readonly Dictionary<SensorRole, float> _nominal = new()
    {
        [SensorRole.Rail12V] = 12.19f,
        [SensorRole.Rail5V] = 5f,
        [SensorRole.Rail3V3] = 3.3f,
    };

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
    public void A_rail_outside_the_ATX_range_is_strong_evidence_for_the_power_supply(SensorRole role, float bad, string rail, string min, string max)
    {
        var builder = AllRails().Series(role, _nominal[role], bad, _nominal[role]);

        var finding = Evaluate(builder).ShouldHaveSingleItem();

        finding.Summary().ShouldBe([RuleAssertions.For("rail.out_of_tolerance", Hypothesis.PowerSupply, EvidenceStrength.Strong)]);
        finding.Parameters.ShouldBe(new Dictionary<string, string>
        {
            ["rail"] = rail,
            ["min"] = min,
            ["max"] = max,
            ["observed"] = bad.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
        }, ignoreOrder: true);
    }

    [Fact]
    public void Rails_within_range_weigh_weakly_against_the_power_supply_with_the_precision_limitation() =>
        Evaluate(AllRails()).Summary().ShouldBe(
        [
            RuleAssertions.Against("rail.in_tolerance", Hypothesis.PowerSupply, EvidenceStrength.Weak),
            RuleAssertions.Limitation("rail.precision"),
        ]);

    [Fact]
    public void Missing_rails_are_limitations()
    {
        var findings = Evaluate(new EvidenceBuilder().Constant(SensorRole.Rail3V3, 3.3f, 5));

        findings.Where(f => f.Code == "sensor.unavailable").Select(f => f.Parameters["role"]).ShouldBe(["Rail12V", "Rail5V"]);
    }
}

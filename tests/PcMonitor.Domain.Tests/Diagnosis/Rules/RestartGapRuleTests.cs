using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class RestartGapRuleTests
{
    private static List<Finding> Evaluate(EvidenceBuilder builder) => [.. new RestartGapRule().Evaluate(builder.Build())];

    [Fact]
    public void Coming_back_within_three_minutes_is_an_observation()
    {
        var finding = Evaluate(new EvidenceBuilder { LoggedAt = EvidenceBuilder.End.AddSeconds(95) }.WithTelemetry()).ShouldHaveSingleItem();

        finding.Summary().ShouldBe([RuleAssertions.Observation("restart.quick")]);
        finding.Parameters["seconds"].ShouldBe("95");
    }

    [Fact]
    public void Staying_off_for_fifteen_minutes_or_more_is_an_observation()
    {
        var finding = Evaluate(new EvidenceBuilder { LoggedAt = EvidenceBuilder.End.AddMinutes(40) }.WithTelemetry()).ShouldHaveSingleItem();

        finding.Summary().ShouldBe([RuleAssertions.Observation("restart.stayed_off")]);
        finding.Parameters["minutes"].ShouldBe("40");
    }

    [Fact]
    public void A_gap_between_three_and_fifteen_minutes_says_nothing() =>
        Evaluate(new EvidenceBuilder { LoggedAt = EvidenceBuilder.End.AddMinutes(8) }.WithTelemetry()).ShouldBeEmpty();

    [Fact]
    public void A_negative_gap_from_a_clock_adjustment_says_nothing() =>
        Evaluate(new EvidenceBuilder { LoggedAt = EvidenceBuilder.End.AddSeconds(-30) }.WithTelemetry()).ShouldBeEmpty();

    [Theory]
    [InlineData(FailureTimeSource.LastAliveEventLog)]
    [InlineData(FailureTimeSource.LastFrameBeforeSleep)]
    [InlineData(FailureTimeSource.Unknown)]
    public void Restart_gap_rule_ignores_lower_bound_or_unknown_failure_times(FailureTimeSource source) =>
        Evaluate(new EvidenceBuilder { Source = source, LastAliveAt = EvidenceBuilder.End, LoggedAt = EvidenceBuilder.End.AddSeconds(30) }.WithTelemetry())
            .ShouldBeEmpty();
}

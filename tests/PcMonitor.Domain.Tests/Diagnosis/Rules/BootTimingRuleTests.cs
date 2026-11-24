using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class BootTimingRuleTests
{
    [Fact]
    public void Last_alive_equal_to_the_event_log_start_means_it_failed_before_the_first_heartbeat()
    {
        var boot = EvidenceBuilder.End.AddMinutes(-3);
        var builder = new EvidenceBuilder { LastAliveAt = boot, Source = FailureTimeSource.LastAliveEventLog }.Event("EventLog", 6005, boot.AddSeconds(1));

        new BootTimingRule().Evaluate(builder.Build()).Summary().ShouldBe([RuleAssertions.Observation("boot.early_failure")]);
    }

    [Fact]
    public void Last_alive_after_a_heartbeat_says_nothing()
    {
        var builder = new EvidenceBuilder { LastAliveAt = EvidenceBuilder.End, Source = FailureTimeSource.LastAliveEventLog }
            .Event("EventLog", 6005, EvidenceBuilder.End.AddMinutes(-40));

        new BootTimingRule().Evaluate(builder.Build()).ShouldBeEmpty();
    }
}

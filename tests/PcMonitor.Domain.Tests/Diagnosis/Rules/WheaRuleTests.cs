using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class WheaRuleTests
{
    [Fact]
    public void A_related_WHEA_event_is_strong_evidence_for_cpu_or_memory()
    {
        var builder = new EvidenceBuilder().Event("Microsoft-Windows-WHEA-Logger", 18, EvidenceBuilder.End)
            .Event("Microsoft-Windows-WHEA-Logger", 19, EvidenceBuilder.End);

        var finding = new WheaRule().Evaluate(builder.Build()).ShouldHaveSingleItem();

        finding.Summary().ShouldBe([RuleAssertions.For("whea.event", Hypothesis.CpuOrMemory, EvidenceStrength.Strong)]);
        (finding.Parameters["count"], finding.Parameters["ids"]).ShouldBe(("2", "18,19"));
    }

    [Fact]
    public void Boot_errors_counted_by_Windows_are_moderate_evidence()
    {
        var builder = new EvidenceBuilder();
        builder.Data["WHEABootErrorCount"] = "2";

        new WheaRule().Evaluate(builder.Build()).Summary()
            .ShouldBe([RuleAssertions.For("whea.boot_errors", Hypothesis.CpuOrMemory, EvidenceStrength.Moderate)]);
    }

    [Fact]
    public void No_WHEA_says_nothing() => new WheaRule().Evaluate(new EvidenceBuilder().Build()).ShouldBeEmpty();
}

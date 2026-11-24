using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class BugcheckRuleTests
{
    private static List<Finding> Evaluate(long code) => [.. new BugcheckRule().Evaluate(new EvidenceBuilder { BugcheckCode = code }.Build())];

    [Fact]
    public void No_bugcheck_weighs_weakly_against_software() =>
        Evaluate(0).Summary().ShouldBe([RuleAssertions.Against("bugcheck.none", Hypothesis.Software, EvidenceStrength.Weak)]);

    [Fact]
    public void A_bugcheck_is_strong_evidence_for_software()
    {
        var finding = Evaluate(0xD1).ShouldHaveSingleItem();

        finding.Summary().ShouldBe([RuleAssertions.For("bugcheck", Hypothesis.Software, EvidenceStrength.Strong)]);
        finding.Parameters["code"].ShouldBe("0x000000D1");
    }

    [Fact]
    public void A_WHEA_bugcheck_is_strong_evidence_for_cpu_or_memory() =>
        Evaluate(0x124).Summary().ShouldBe([RuleAssertions.For("bugcheck.whea", Hypothesis.CpuOrMemory, EvidenceStrength.Strong)]);
}

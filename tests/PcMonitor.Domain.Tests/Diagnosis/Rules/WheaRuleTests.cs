using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class WheaRuleTests
{
    private const string Whea = "Microsoft-Windows-WHEA-Logger";

    private static List<Finding> Evaluate(EvidenceBuilder builder) => [.. new WheaRule().Evaluate(builder.Build())];

    [Fact]
    public void A_fatal_WHEA_event_is_strong_evidence_for_cpu_or_memory_with_the_power_caveat()
    {
        var findings = Evaluate(new EvidenceBuilder().Event(Whea, 18, EvidenceBuilder.End));

        findings.Summary().ShouldBe(
        [
            RuleAssertions.For("whea.hardware_error", Hypothesis.CpuOrMemory, EvidenceStrength.Strong),
            RuleAssertions.Limitation("whea.may_be_power"),
        ]);
        findings[0].Parameters["sources"].ShouldBe("event:18");
    }

    [Fact]
    public void A_machine_check_seen_three_ways_counts_once()
    {
        var builder = new EvidenceBuilder { BugcheckCode = 0x124 }.Event(Whea, 18, EvidenceBuilder.End);
        builder.Data["WHEABootErrorCount"] = "2";

        var finding = Evaluate(builder).Single(f => f.Kind == FindingKind.Evidence);

        (finding.Strength, finding.Parameters["sources"]).ShouldBe((EvidenceStrength.Strong, "bugcheck:0x00000124,event:18,boot_errors:2"));
    }

    [Fact]
    public void Corrected_errors_are_weak_evidence() =>
        Evaluate(new EvidenceBuilder().Event(Whea, 19, EvidenceBuilder.End)).Single(f => f.Kind == FindingKind.Evidence)
            .Strength.ShouldBe(EvidenceStrength.Weak);

    [Fact]
    public void Boot_errors_counted_by_Windows_are_moderate_evidence()
    {
        var builder = new EvidenceBuilder();
        builder.Data["WHEABootErrorCount"] = "2";

        Evaluate(builder).Single(f => f.Kind == FindingKind.Evidence).Strength.ShouldBe(EvidenceStrength.Moderate);
    }

    [Fact]
    public void A_PCIe_error_is_an_observation_not_cpu_or_memory_evidence() =>
        Evaluate(new EvidenceBuilder().Event(Whea, 17, EvidenceBuilder.End)).Summary()
            .ShouldBe([RuleAssertions.Observation("whea.pcie")]);

    [Fact]
    public void No_WHEA_says_nothing() => Evaluate(new EvidenceBuilder()).ShouldBeEmpty();
}

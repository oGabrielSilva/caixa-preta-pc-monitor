using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

internal static class RuleAssertions
{
    /// <summary>Findings as comparable tuples: code, kind, hypothesis, supports, strength.</summary>
    public static List<(string, FindingKind, Hypothesis?, bool, EvidenceStrength)> Summary(this IEnumerable<Finding> findings) =>
        [.. findings.Select(f => (f.Code, f.Kind, f.Hypothesis, f.Supports, f.Strength))];

    public static List<(string, FindingKind, Hypothesis?, bool, EvidenceStrength)> Summary(this Finding finding) => Summary([finding]);

    public static (string, FindingKind, Hypothesis?, bool, EvidenceStrength) For(string code, Hypothesis h, EvidenceStrength s) =>
        (code, FindingKind.Evidence, h, true, s);

    public static (string, FindingKind, Hypothesis?, bool, EvidenceStrength) Against(string code, Hypothesis h, EvidenceStrength s) =>
        (code, FindingKind.Evidence, h, false, s);

    public static (string, FindingKind, Hypothesis?, bool, EvidenceStrength) Limitation(string code) =>
        (code, FindingKind.Limitation, null, true, EvidenceStrength.Weak);

    public static (string, FindingKind, Hypothesis?, bool, EvidenceStrength) Observation(string code) =>
        (code, FindingKind.Observation, null, true, EvidenceStrength.Weak);
}

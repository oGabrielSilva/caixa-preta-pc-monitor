namespace PcMonitor.Domain.Diagnosis;

/// <summary>
/// One result of a rule. <see cref="Code"/> and <see cref="Parameters"/> are stable identifiers translated by the user
/// interface; the domain never produces display text.
/// </summary>
public sealed record Finding(
    string Code,
    FindingKind Kind,
    Hypothesis? Hypothesis,
    bool Supports,
    EvidenceStrength Strength,
    IReadOnlyDictionary<string, string> Parameters)
{
    private static readonly IReadOnlyDictionary<string, string> _none = new Dictionary<string, string>();

    /// <summary>Score contribution: positive for, negative against, zero for observations and limitations.</summary>
    public int Weight => Kind == FindingKind.Evidence ? (Supports ? 1 : -1) * (int)Strength : 0;

    public static Finding For(string code, Hypothesis hypothesis, EvidenceStrength strength, IReadOnlyDictionary<string, string>? parameters = null) =>
        new(code, FindingKind.Evidence, hypothesis, true, strength, parameters ?? _none);

    public static Finding Against(string code, Hypothesis hypothesis, EvidenceStrength strength, IReadOnlyDictionary<string, string>? parameters = null) =>
        new(code, FindingKind.Evidence, hypothesis, false, strength, parameters ?? _none);

    public static Finding Observation(string code, IReadOnlyDictionary<string, string>? parameters = null) =>
        new(code, FindingKind.Observation, null, true, EvidenceStrength.Weak, parameters ?? _none);

    public static Finding Limitation(string code, IReadOnlyDictionary<string, string>? parameters = null) =>
        new(code, FindingKind.Limitation, null, true, EvidenceStrength.Weak, parameters ?? _none);

    /// <summary>Same code and parameters (records compare dictionaries by reference, so this compares contents).</summary>
    public bool SameAs(Finding other) =>
        Code == other?.Code && Kind == other.Kind && Parameters.Count == other.Parameters.Count
        && Parameters.All(p => other.Parameters.TryGetValue(p.Key, out var value) && value == p.Value);
}

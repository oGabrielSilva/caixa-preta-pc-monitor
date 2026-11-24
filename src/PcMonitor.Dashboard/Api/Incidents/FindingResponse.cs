using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Dashboard.Api.Incidents;

/// <summary>One result of a diagnosis rule. The dashboard translates the code and its parameters.</summary>
/// <param name="Code">Stable code.</param>
/// <param name="Kind">Evidence (scores), observation or limitation.</param>
/// <param name="Hypothesis">Hypothesis the evidence is about; <c>null</c> for observations and limitations.</param>
/// <param name="Supports">For or against the hypothesis.</param>
/// <param name="Strength">Weight of the evidence.</param>
/// <param name="Parameters">Values the text needs, as invariant strings (for example <c>observed</c> = <c>11.28</c>).</param>
public sealed record FindingResponse(
    FindingCode Code,
    FindingKind Kind,
    Hypothesis? Hypothesis,
    bool Supports,
    EvidenceStrength Strength,
    IReadOnlyDictionary<string, string> Parameters)
{
    public static FindingResponse From(Finding finding) =>
        new(new FindingCode(finding.Code), finding.Kind, finding.Hypothesis, finding.Supports, finding.Strength, finding.Parameters);
}

using PcMonitor.Application.Incidents;

namespace PcMonitor.Dashboard.Api.Incidents;

/// <summary>The stored diagnosis of an incident.</summary>
/// <param name="EngineVersion">Version of the rules that produced it.</param>
/// <param name="DiagnosedAt">When it was computed.</param>
/// <param name="Ranking">Hypotheses by score, highest first.</param>
/// <param name="Findings">Evidence and observations.</param>
/// <param name="Limitations">What this case cannot tell.</param>
public sealed record DiagnosisResponse(
    int EngineVersion,
    DateTimeOffset DiagnosedAt,
    IReadOnlyList<HypothesisScoreResponse> Ranking,
    IReadOnlyList<FindingResponse> Findings,
    IReadOnlyList<FindingResponse> Limitations)
{
    public static DiagnosisResponse From(StoredDiagnosis diagnosis) => new(
        diagnosis.EngineVersion,
        diagnosis.DiagnosedAt,
        [.. diagnosis.Ranking.Select(s => new HypothesisScoreResponse(s.Hypothesis, s.Score))],
        [.. diagnosis.Findings.Select(FindingResponse.From)],
        [.. diagnosis.Limitations.Select(FindingResponse.From)]);
}

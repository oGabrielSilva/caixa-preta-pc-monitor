namespace PcMonitor.Domain.Diagnosis;

/// <summary>The explainable result for one incident.</summary>
/// <param name="EngineVersion">Version of the rules that produced it (<see cref="DiagnosisEngine.Version"/>).</param>
/// <param name="Ranking">Hypotheses with a non-zero score, highest first.</param>
/// <param name="Confidence">How much the top hypothesis stands out.</param>
/// <param name="Findings">Evidence and observations, in rule order.</param>
/// <param name="Limitations">What this diagnosis cannot tell, without duplicates.</param>
public sealed record IncidentDiagnosis(
    int EngineVersion,
    IReadOnlyList<HypothesisScore> Ranking,
    Confidence Confidence,
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<Finding> Limitations)
{
    public Hypothesis? Top => Ranking.Count > 0 && Ranking[0].Score > 0 ? Ranking[0].Hypothesis : null;
}

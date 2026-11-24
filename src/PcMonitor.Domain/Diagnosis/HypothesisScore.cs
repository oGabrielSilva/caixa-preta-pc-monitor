namespace PcMonitor.Domain.Diagnosis;

/// <summary>Total weight of the evidence for (positive) and against (negative) a hypothesis.</summary>
public sealed record HypothesisScore(Hypothesis Hypothesis, int Score);

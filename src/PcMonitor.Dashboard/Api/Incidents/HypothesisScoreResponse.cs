using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Dashboard.Api.Incidents;

/// <summary>Sum of the evidence for one hypothesis.</summary>
public sealed record HypothesisScoreResponse(Hypothesis Hypothesis, int Score);

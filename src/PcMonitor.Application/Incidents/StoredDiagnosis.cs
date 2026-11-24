using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Application.Incidents;

/// <summary>A diagnosis as stored by the collector.</summary>
public sealed record StoredDiagnosis(
    int EngineVersion,
    DateTimeOffset DiagnosedAt,
    IReadOnlyList<HypothesisScore> Ranking,
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<Finding> Limitations);

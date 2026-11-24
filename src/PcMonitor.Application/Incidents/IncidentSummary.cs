using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Application.Incidents;

/// <summary>One unexpected shutdown as listed by the dashboard.</summary>
/// <remarks>
/// <see cref="Confidence"/>: <c>null</c> while the incident has no diagnosis yet.<br/>
/// <see cref="HasTelemetry"/>: The collector recorded frames in the session the shutdown ended.<br/>
/// <see cref="ObservationCodes"/>: Codes of the diagnosis observations (for example <c>boot.early_failure</c>).<br/>
/// </remarks>
public sealed record IncidentSummary(
    long Id,
    DateTimeOffset LoggedAt,
    DateTimeOffset? EstimatedFailureAt,
    FailureTimeSource Source,
    int SleepState,
    long BugcheckCode,
    bool HasTelemetry,
    Hypothesis? TopHypothesis,
    Confidence? Confidence,
    IReadOnlyList<string> ObservationCodes);

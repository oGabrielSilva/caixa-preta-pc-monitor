using PcMonitor.Application.Incidents;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Dashboard.Api.Incidents;

/// <summary>One unexpected shutdown in the list.</summary>
/// <param name="Id">Incident id.</param>
/// <param name="LoggedAt">When Windows logged the shutdown (event 41, at the next boot).</param>
/// <param name="EstimatedFailureAt">Best estimate of when the machine went down.</param>
/// <param name="TimeSource">Where the estimate comes from: <c>LastFrame</c> is within about a second.</param>
/// <param name="SleepState">Power state in transition at the failure: 0 none, 3 sleep, 4 hibernation/Fast Startup.</param>
/// <param name="BugcheckCode">Blue screen code; 0 when there was none.</param>
/// <param name="HasTelemetry">The collector recorded the session the shutdown ended.</param>
/// <param name="TopHypothesis">Most likely cause; <c>null</c> when inconclusive or not diagnosed yet.</param>
/// <param name="Confidence">Confidence of the diagnosis; <c>null</c> when not diagnosed yet.</param>
/// <param name="Observations">Codes of the neutral observations (for example <c>boot.early_failure</c>).</param>
public sealed record IncidentSummaryResponse(
    long Id,
    DateTimeOffset LoggedAt,
    DateTimeOffset? EstimatedFailureAt,
    FailureTimeSource TimeSource,
    int SleepState,
    long BugcheckCode,
    bool HasTelemetry,
    Hypothesis? TopHypothesis,
    Confidence? Confidence,
    IReadOnlyList<FindingCode> Observations)
{
    public static IncidentSummaryResponse From(IncidentSummary summary) => new(
        summary.Id,
        summary.LoggedAt,
        summary.EstimatedFailureAt,
        summary.Source,
        summary.SleepState,
        summary.BugcheckCode,
        summary.HasTelemetry,
        summary.TopHypothesis,
        summary.Confidence,
        [.. summary.ObservationCodes.Select(code => new FindingCode(code))]);
}

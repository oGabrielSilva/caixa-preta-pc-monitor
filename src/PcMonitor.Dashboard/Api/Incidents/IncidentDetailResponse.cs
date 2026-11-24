using PcMonitor.Application.Incidents;

namespace PcMonitor.Dashboard.Api.Incidents;

/// <summary>Everything about one incident.</summary>
/// <param name="Summary">The list entry.</param>
/// <param name="LastAliveAt">Last sign of life Windows recorded (event 6008).</param>
/// <param name="TelemetryEndAt">Last frame the collector recorded in that session.</param>
/// <param name="Events">Windows events this incident owns, oldest first.</param>
/// <param name="Diagnosis"><c>null</c> until the collector diagnoses it.</param>
/// <param name="Telemetry"><c>null</c> without telemetry.</param>
public sealed record IncidentDetailResponse(
    IncidentSummaryResponse Summary,
    DateTimeOffset? LastAliveAt,
    DateTimeOffset? TelemetryEndAt,
    IReadOnlyList<EventResponse> Events,
    DiagnosisResponse? Diagnosis,
    TelemetryResponse? Telemetry)
{
    public static IncidentDetailResponse From(IncidentDetail detail) => new(
        IncidentSummaryResponse.From(detail.Summary),
        detail.LastAliveAt,
        detail.TelemetryEndAt,
        [.. detail.Events.Select(e => new EventResponse(e.Provider, e.EventId, e.LoggedAt, e.Level))],
        detail.Diagnosis is null ? null : DiagnosisResponse.From(detail.Diagnosis),
        detail.Telemetry is null ? null : TelemetryResponse.From(detail.Telemetry, detail.TelemetryAtFailure));
}

using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Application.Incidents;

/// <summary>Everything the dashboard shows about one incident.</summary>
/// <remarks>
/// <see cref="Events"/>: Only the events this incident owns (<see cref="Domain.Incidents.RelatedEventScope"/>), oldest first.<br/>
/// <see cref="Telemetry"/>: The same 120 s window the diagnosis evaluated; <c>null</c> without telemetry.<br/>
/// </remarks>
public sealed record IncidentDetail(
    IncidentSummary Summary,
    DateTimeOffset? LastAliveAt,
    DateTimeOffset? TelemetryEndAt,
    IReadOnlyList<RelatedEventView> Events,
    StoredDiagnosis? Diagnosis,
    TelemetryWindow? Telemetry);

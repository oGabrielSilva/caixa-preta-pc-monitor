using PcMonitor.Domain.Incidents;

namespace PcMonitor.Domain.Diagnosis;

/// <summary>Everything the rules look at for one incident.</summary>
/// <param name="IncidentId">Stored incident id.</param>
/// <param name="Incident">The incident.</param>
/// <param name="Telemetry">The recorded seconds before the failure, or <c>null</c> without telemetry.</param>
/// <param name="RelatedEvents">Event log entries attributed to this incident only.</param>
public sealed record IncidentEvidence(long IncidentId, Incident Incident, TelemetryWindow? Telemetry, IReadOnlyList<SystemEvent> RelatedEvents);

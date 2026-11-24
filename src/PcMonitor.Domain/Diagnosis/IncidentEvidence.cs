using PcMonitor.Domain.Incidents;

namespace PcMonitor.Domain.Diagnosis;

/// <summary>Everything the rules look at for one incident. Related events are <see cref="Incident.RelatedEvents"/>.</summary>
/// <param name="IncidentId">Stored incident id.</param>
/// <param name="Incident">The incident, with only the related events attributed to it.</param>
/// <param name="Telemetry">The recorded seconds before the end of telemetry, or <c>null</c> without telemetry.</param>
public sealed record IncidentEvidence(long IncidentId, Incident Incident, TelemetryWindow? Telemetry)
{
    /// <summary>
    /// The telemetry only when it ends at the failure (<see cref="FailureTimeSource.LastFrame"/>). When the collector had
    /// stopped earlier or the machine was asleep, the last recorded seconds say nothing about the moment it went down.
    /// </summary>
    public TelemetryWindow? AtFailure => Incident.FailureTimeSource == FailureTimeSource.LastFrame ? Telemetry : null;
}

namespace PcMonitor.Domain.Incidents;

/// <summary>An unexpected shutdown with what is known around it.</summary>
/// <param name="Shutdown">The Kernel-Power 41 event that defines the incident (ADR 0005).</param>
/// <param name="EstimatedFailureAt">Best estimate of when the machine went down.</param>
/// <param name="FailureTimeSource">Where the estimate comes from.</param>
/// <param name="SessionId">Monitoring session that was running, when there was one.</param>
/// <param name="SegmentId">Segment holding that session's last frame.</param>
/// <param name="TelemetryEndAt">Time of the session's last frame: the end of the telemetry kept for this incident.</param>
/// <param name="RelatedEvents">Event log entries around the failure (shutdowns, bugchecks, WHEA, power).</param>
public sealed record Incident(
    UnexpectedShutdown Shutdown,
    DateTimeOffset? EstimatedFailureAt,
    FailureTimeSource FailureTimeSource,
    long? SessionId,
    long? SegmentId,
    DateTimeOffset? TelemetryEndAt,
    IReadOnlyList<SystemEvent> RelatedEvents);

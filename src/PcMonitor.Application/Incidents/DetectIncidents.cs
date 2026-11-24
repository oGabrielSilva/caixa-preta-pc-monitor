using Microsoft.Extensions.Logging;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Incidents;

/// <summary>
/// Turns every Kernel-Power 41 event not yet saved into an incident (ADR 0005): links it to the monitoring session
/// that was running, estimates when the machine went down, attaches related event log entries and closes the
/// session. Sessions left open with no matching event are closed as <see cref="EndReason.Abnormal"/>: the
/// collector itself died, not the machine. Run it before a new session starts.
/// </summary>
public sealed partial class DetectIncidents(ISystemEventLog eventLog, IIncidentStore store, ILogger<DetectIncidents> logger)
{
    private static readonly TimeSpan _relatedBefore = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _relatedAfter = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan _searchWithoutEstimate = TimeSpan.FromMinutes(30);

    public DetectionResult Run()
    {
        var shutdowns = eventLog.ReadUnexpectedShutdowns(store.LatestIncidentLoggedAt());
        var unmatched = store.OpenSessions().ToList();
        var incidents = 0;
        var withTelemetry = 0;

        foreach (var shutdown in shutdowns)
        {
            // The session that went down started before the event was logged (at the next boot); when several
            // did, it is the latest one. Each session belongs to at most one shutdown.
            var session = unmatched.LastOrDefault(s => s.StartedAt < shutdown.LoggedAt);
            if (session is not null)
            {
                unmatched.Remove(session);
            }

            var (estimate, source) = EstimateFailure(shutdown, session);
            var related = eventLog.ReadRelated(
                (estimate ?? shutdown.LoggedAt - _searchWithoutEstimate) - _relatedBefore, shutdown.LoggedAt + _relatedAfter);

            if (!store.SaveIncident(new Incident(shutdown, estimate, source, session?.SessionId, session?.LastSegmentId, related)))
            {
                continue;
            }

            incidents++;
            if (session is not null)
            {
                withTelemetry++;
                store.EndSession(session, session.LastFrameAt ?? session.StartedAt, EndReason.UnexpectedShutdown);
            }

            LogIncident(logger, shutdown.LoggedAt, estimate, source, session is not null);
        }

        foreach (var session in unmatched)
        {
            store.EndSession(session, session.LastFrameAt ?? session.StartedAt, EndReason.Abnormal);
            LogAbnormalSession(logger, session.SessionId);
        }

        return new DetectionResult(incidents, withTelemetry, unmatched.Count);
    }

    private static (DateTimeOffset? Estimate, FailureTimeSource Source) EstimateFailure(UnexpectedShutdown shutdown, OpenSession? session) =>
        session?.LastFrameAt is { } lastFrame ? (lastFrame, FailureTimeSource.LastFrame)
        : shutdown.LastAliveAt is { } lastAlive ? (lastAlive, FailureTimeSource.LastAliveEventLog)
        : (null, FailureTimeSource.Unknown);

    [LoggerMessage(EventId = 200, Level = LogLevel.Warning,
        Message = "Unexpected shutdown logged at {LoggedAt}: estimated failure {Estimate} ({Source}); telemetry recorded: {WithTelemetry}.")]
    private static partial void LogIncident(
        ILogger logger, DateTimeOffset loggedAt, DateTimeOffset? estimate, FailureTimeSource source, bool withTelemetry);

    [LoggerMessage(EventId = 201, Level = LogLevel.Warning,
        Message = "Monitoring session {SessionId} ended without a reason and Windows logged no crash: the collector stopped abnormally.")]
    private static partial void LogAbnormalSession(ILogger logger, long sessionId);
}

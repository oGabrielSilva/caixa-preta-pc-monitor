using Microsoft.Extensions.Logging;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Incidents;

/// <summary>
/// Turns every Kernel-Power 41 event not yet saved into an incident (ADR 0005): links it to the monitoring session of
/// the boot that went down, estimates when it failed, attaches the event log entries around it and closes that session
/// in the same transaction. Sessions left open with no matching event are closed as <see cref="EndReason.Abnormal"/>:
/// the collector died, not the machine. Run it before a new session starts.
/// </summary>
public sealed partial class DetectIncidents(
    ISystemEventLog eventLog, IIncidentReader reader, IIncidentWriter writer, ILogger<DetectIncidents> logger)
{
    private static readonly TimeSpan _relatedBefore = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _relatedAfter = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan _searchWithoutEstimate = TimeSpan.FromMinutes(30);

    /// <summary>A session's boot time is measured from uptime; its 41 is logged a few seconds after that boot.</summary>
    private static readonly TimeSpan _bootTolerance = TimeSpan.FromMinutes(1);

    /// <summary>Windows "alive" later than the last frame by more than this: the collector had stopped before the failure.</summary>
    private static readonly TimeSpan _recordingSlack = TimeSpan.FromSeconds(5);

    public DetectionResult Run()
    {
        // Read everything each time (cheap) instead of a time watermark: a reset RTC can stamp a new 41 in the past.
        var shutdowns = eventLog.ReadUnexpectedShutdowns();
        var saved = reader.SavedIncidents();
        var unmatched = reader.OpenSessions().ToList();
        var incidents = 0;
        var withTelemetry = 0;

        for (var i = 0; i < shutdowns.Count; i++)
        {
            var shutdown = shutdowns[i];
            if (saved.Contains(IncidentKey.Of(shutdown)))
            {
                continue;
            }

            var previous = i > 0 ? shutdowns[i - 1] : null;
            var next = i + 1 < shutdowns.Count ? shutdowns[i + 1] : null;
            try
            {
                var session = SessionThatWentDown(unmatched, shutdown, previous);
                var incident = BuildIncident(shutdown, session, previous, next);
                var closure = session is null ? null : new SessionClosure(session, session.LastFrameAt ?? session.StartedAt, EndReason.UnexpectedShutdown);

                writer.SaveIncident(incident, closure);

                if (session is not null)
                {
                    unmatched.Remove(session);
                }

                incidents++;
                withTelemetry += incident.TelemetryEndAt is null ? 0 : 1;
                LogIncident(logger, shutdown.LoggedAt, incident.EstimatedFailureAt, incident.FailureTimeSource, incident.TelemetryEndAt is not null);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                LogIncidentFailed(logger, exception, shutdown.RecordId);
            }
        }

        var abnormal = 0;
        foreach (var session in unmatched)
        {
            try
            {
                writer.CloseSession(new SessionClosure(session, session.LastFrameAt ?? session.StartedAt, EndReason.Abnormal));
                abnormal++;
                LogAbnormalSession(logger, session.SessionId);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                LogCloseFailed(logger, exception, session.SessionId);
            }
        }

        return new DetectionResult(incidents, withTelemetry, abnormal);
    }

    /// <summary>
    /// The latest open session whose boot is the one that ended with <paramref name="shutdown"/>: booted before it was
    /// logged and not before the previous crash's boot.
    /// </summary>
    private static OpenSession? SessionThatWentDown(List<OpenSession> open, UnexpectedShutdown shutdown, UnexpectedShutdown? previous) =>
        open.LastOrDefault(s => s.BootedAt < shutdown.LoggedAt
            && (previous is null || s.BootedAt > previous.LoggedAt - _bootTolerance));

    private Incident BuildIncident(UnexpectedShutdown shutdown, OpenSession? session, UnexpectedShutdown? previous, UnexpectedShutdown? next)
    {
        var (estimate, source) = EstimateFailure(shutdown, session?.LastFrameAt);

        var since = (estimate ?? shutdown.LoggedAt - _searchWithoutEstimate) - _relatedBefore;
        var until = shutdown.LoggedAt + _relatedAfter;
        if (previous is not null && since <= previous.LoggedAt)
        {
            since = previous.LoggedAt + TimeSpan.FromTicks(1);
        }

        if (next is not null && until >= next.LoggedAt)
        {
            until = next.LoggedAt - TimeSpan.FromTicks(1);
        }

        return new Incident(shutdown, estimate, source, session?.SessionId, session?.LastSegmentId, session?.LastFrameAt,
            eventLog.ReadRelated(since, until));
    }

    private static (DateTimeOffset? Estimate, FailureTimeSource Source) EstimateFailure(UnexpectedShutdown shutdown, DateTimeOffset? lastFrame)
    {
        if (lastFrame is { } frame && !(shutdown.LastAliveAt > frame + _recordingSlack))
        {
            return (frame, shutdown.SleepInProgress ? FailureTimeSource.LastFrameBeforeSleep : FailureTimeSource.LastFrame);
        }

        return shutdown.LastAliveAt is { } lastAlive
            ? (lastAlive, FailureTimeSource.LastAliveEventLog)
            : (null, FailureTimeSource.Unknown);
    }

    [LoggerMessage(EventId = 200, Level = LogLevel.Warning,
        Message = "Unexpected shutdown logged at {LoggedAt}: estimated failure {Estimate} ({Source}); telemetry recorded: {WithTelemetry}.")]
    private static partial void LogIncident(
        ILogger logger, DateTimeOffset loggedAt, DateTimeOffset? estimate, FailureTimeSource source, bool withTelemetry);

    [LoggerMessage(EventId = 201, Level = LogLevel.Warning,
        Message = "Monitoring session {SessionId} ended without a reason and Windows logged no crash: the collector stopped abnormally.")]
    private static partial void LogAbnormalSession(ILogger logger, long sessionId);

    [LoggerMessage(EventId = 202, Level = LogLevel.Error, Message = "Importing the unexpected shutdown record {RecordId} failed; it will be retried at the next start.")]
    private static partial void LogIncidentFailed(ILogger logger, Exception exception, long recordId);

    [LoggerMessage(EventId = 203, Level = LogLevel.Error, Message = "Closing the abnormal session {SessionId} failed; it will be retried at the next start.")]
    private static partial void LogCloseFailed(ILogger logger, Exception exception, long sessionId);
}

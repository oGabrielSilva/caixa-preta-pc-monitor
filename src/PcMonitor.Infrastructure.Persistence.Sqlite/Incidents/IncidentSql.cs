namespace PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;

/// <summary>SQL for incidents, their related events and orphaned sessions.</summary>
internal static class IncidentSql
{
    public const string LatestLoggedAt = "SELECT MAX(logged_at_ms) FROM incident;";

    public const string OpenSessions = """
        SELECT s.id AS SessionId,
               s.started_at_ms AS StartedAtMs,
               (SELECT g.id FROM segment g WHERE g.session_id = s.id ORDER BY g.started_at_ms DESC, g.id DESC LIMIT 1) AS LastSegmentId,
               (SELECT g.id FROM segment g WHERE g.session_id = s.id AND g.end_reason IS NULL ORDER BY g.id DESC LIMIT 1) AS OpenSegmentId,
               (SELECT MAX(g.last_frame_ms) FROM segment g WHERE g.session_id = s.id) AS LastFrameMs
        FROM session s
        WHERE s.end_reason IS NULL
        ORDER BY s.started_at_ms, s.id;
        """;

    /// <summary>Returns no row when the shutdown event is already saved.</summary>
    public const string InsertIncident = """
        INSERT INTO incident (event_record_id, logged_at_ms, estimated_failure_ms, failure_time_source, session_id, segment_id,
                              bugcheck_code, sleep_in_progress, power_button_ms, long_power_button, last_alive_ms, data_json)
        VALUES (@RecordId, @LoggedAtMs, @EstimatedFailureMs, @FailureTimeSource, @SessionId, @SegmentId,
                @BugcheckCode, @SleepInProgress, @PowerButtonMs, @LongPowerButton, @LastAliveMs, @DataJson)
        ON CONFLICT (event_record_id, logged_at_ms) DO NOTHING
        RETURNING id;
        """;

    public const string InsertEvent = """
        INSERT OR IGNORE INTO incident_event (incident_id, provider, event_id, record_id, logged_at_ms, level, data_json)
        VALUES (@IncidentId, @Provider, @EventId, @RecordId, @LoggedAtMs, @Level, @DataJson);
        """;

    public const string EndSegment = "UPDATE segment SET ended_at_ms = @AtMs, end_reason = @Reason WHERE id = @SegmentId;";

    public const string EndSession = "UPDATE session SET ended_at_ms = @AtMs, end_reason = @Reason WHERE id = @SessionId;";
}

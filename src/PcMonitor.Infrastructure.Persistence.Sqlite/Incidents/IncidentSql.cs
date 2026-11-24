namespace PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;

/// <summary>SQL for incidents and their related events. Ending sessions reuses <see cref="Monitoring.MonitoringSql"/>.</summary>
internal static class IncidentSql
{
    public const string SavedKeys = "SELECT event_record_id AS RecordId, logged_at_ms AS LoggedAtMs FROM incident;";

    /// <summary>Segments are ordered by id (insertion order), never by wall-clock time, which can be adjusted.</summary>
    public const string OpenSessions = """
        SELECT s.id AS SessionId,
               s.started_at_ms AS StartedAtMs,
               s.booted_at_ms AS BootedAtMs,
               COALESCE(
                   (SELECT g.id FROM segment g WHERE g.session_id = s.id AND g.last_frame_ms IS NOT NULL ORDER BY g.id DESC LIMIT 1),
                   (SELECT g.id FROM segment g WHERE g.session_id = s.id ORDER BY g.id DESC LIMIT 1)) AS LastSegmentId,
               (SELECT g.id FROM segment g WHERE g.session_id = s.id AND g.end_reason IS NULL ORDER BY g.id DESC LIMIT 1) AS OpenSegmentId,
               (SELECT g.last_frame_ms FROM segment g WHERE g.session_id = s.id AND g.last_frame_ms IS NOT NULL ORDER BY g.id DESC LIMIT 1) AS LastFrameMs
        FROM session s
        WHERE s.end_reason IS NULL
        ORDER BY s.started_at_ms, s.id;
        """;

    /// <summary>Returns no row when the shutdown event is already saved.</summary>
    public const string InsertIncident = """
        INSERT INTO incident (event_record_id, logged_at_ms, estimated_failure_ms, failure_time_source, session_id, segment_id,
                              telemetry_end_ms, bugcheck_code, sleep_in_progress, power_button_ms, long_power_button,
                              last_alive_ms, data_json)
        VALUES (@RecordId, @LoggedAtMs, @EstimatedFailureMs, @FailureTimeSource, @SessionId, @SegmentId,
                @TelemetryEndMs, @BugcheckCode, @SleepState, @PowerButtonMs, @LongPowerButton,
                @LastAliveMs, @DataJson)
        ON CONFLICT (event_record_id, logged_at_ms) DO NOTHING
        RETURNING id;
        """;

    public const string InsertEvent = """
        INSERT OR IGNORE INTO incident_event (incident_id, provider, event_id, record_id, logged_at_ms, level, data_json)
        VALUES (@IncidentId, @Provider, @EventId, @RecordId, @LoggedAtMs, @Level, @DataJson);
        """;
}

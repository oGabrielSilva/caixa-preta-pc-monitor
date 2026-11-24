namespace PcMonitor.Infrastructure.Persistence.Sqlite.Diagnosis;

/// <summary>SQL for diagnoses and for assembling their inputs.</summary>
internal static class DiagnosisSql
{
    public const string IncidentsNeedingDiagnosis = """
        SELECT i.id AS Id, i.event_record_id AS EventRecordId, i.logged_at_ms AS LoggedAtMs,
               i.estimated_failure_ms AS EstimatedFailureMs, i.failure_time_source AS FailureTimeSource,
               i.session_id AS SessionId, i.segment_id AS SegmentId, i.telemetry_end_ms AS TelemetryEndMs,
               i.bugcheck_code AS BugcheckCode, i.sleep_in_progress AS SleepState, i.power_button_ms AS PowerButtonMs,
               i.long_power_button AS LongPowerButton, i.last_alive_ms AS LastAliveMs, i.data_json AS DataJson
        FROM incident i LEFT JOIN diagnosis d ON d.incident_id = i.id
        WHERE d.incident_id IS NULL OR d.engine_version < @EngineVersion
        ORDER BY i.event_record_id;
        """;

    /// <summary>Every incident in log order, to find each one's neighbours.</summary>
    public const string IncidentTimeline = "SELECT id AS Id, logged_at_ms AS LoggedAtMs FROM incident ORDER BY event_record_id;";

    public const string RelatedEvents = """
        SELECT provider AS Provider, event_id AS EventId, record_id AS RecordId, logged_at_ms AS LoggedAtMs,
               level AS Level, data_json AS DataJson
        FROM incident_event
        WHERE incident_id = @IncidentId AND logged_at_ms > @AfterMs AND logged_at_ms < @BeforeMs
        ORDER BY logged_at_ms, record_id;
        """;

    public const string WindowFrames = """
        SELECT f.segment_id AS SegmentId, f.ts_ms AS TimestampMs, f."values" AS "Values"
        FROM frame f JOIN segment g ON g.id = f.segment_id
        WHERE g.session_id = @SessionId AND f.ts_ms BETWEEN @StartMs AND @EndMs
        ORDER BY f.ts_ms, f.id;
        """;

    public const string SegmentRoles = "SELECT ordinal AS Ordinal, role AS Role FROM segment_sensor WHERE segment_id = @SegmentId;";

    public const string Upsert = """
        INSERT INTO diagnosis (incident_id, engine_version, diagnosed_at_ms, top_hypothesis, confidence,
                               ranking_json, findings_json, limitations_json)
        VALUES (@IncidentId, @EngineVersion, @DiagnosedAtMs, @TopHypothesis, @Confidence,
                @RankingJson, @FindingsJson, @LimitationsJson)
        ON CONFLICT (incident_id) DO UPDATE SET
            engine_version = excluded.engine_version, diagnosed_at_ms = excluded.diagnosed_at_ms,
            top_hypothesis = excluded.top_hypothesis, confidence = excluded.confidence,
            ranking_json = excluded.ranking_json, findings_json = excluded.findings_json,
            limitations_json = excluded.limitations_json;
        """;
}

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;

/// <summary>SQL for sessions, segments, the sensor catalog and frames.</summary>
internal static class MonitoringSql
{
    public const string InsertSession = """
        INSERT INTO session (started_at_ms, booted_at_ms, source_status, board_product)
        VALUES (@StartedAtMs, @BootedAtMs, @SourceStatus, @BoardProduct)
        RETURNING id;
        """;

    public const string InsertSegment = """
        INSERT INTO segment (session_id, started_at_ms) VALUES (@SessionId, @StartedAtMs) RETURNING id;
        """;

    public const string UpsertSensor = """
        INSERT INTO sensor (stable_key, name, kind, unit, role) VALUES (@StableKey, @Name, @Kind, @Unit, @Role)
        ON CONFLICT (stable_key) DO UPDATE SET name = excluded.name, kind = excluded.kind, unit = excluded.unit, role = excluded.role
        RETURNING id;
        """;

    public const string InsertSegmentSensor = """
        INSERT INTO segment_sensor (segment_id, ordinal, sensor_id) VALUES (@SegmentId, @Ordinal, @SensorId);
        """;

    public const string InsertFrame = """
        INSERT INTO frame (segment_id, ts_ms, "values") VALUES (@SegmentId, @TimestampMs, @Values);
        UPDATE segment SET last_frame_ms = @TimestampMs WHERE id = @SegmentId;
        """;

    public const string EndSegment = """
        UPDATE segment SET ended_at_ms = @EndedAtMs, end_reason = @Reason WHERE id = @Id;
        """;

    public const string EndSession = """
        UPDATE session SET ended_at_ms = @EndedAtMs, end_reason = @Reason WHERE id = @Id;
        """;

    public const string SelectCatalog = """
        SELECT s.stable_key AS StableKey, s.name AS Name, s.kind AS Kind, s.unit AS Unit, s.role AS Role
        FROM segment_sensor ss JOIN sensor s ON s.id = ss.sensor_id
        WHERE ss.segment_id = @SegmentId
        ORDER BY ss.ordinal;
        """;

    public const string SelectFrames = """
        SELECT ts_ms AS TimestampMs, "values" AS "Values" FROM frame WHERE segment_id = @SegmentId ORDER BY ts_ms, id;
        """;
}

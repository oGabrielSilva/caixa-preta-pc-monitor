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

    /// <summary>Sensor identity only; the no-op update lets <c>RETURNING</c> yield the id of an existing row.</summary>
    public const string UpsertSensor = """
        INSERT INTO sensor (stable_key) VALUES (@StableKey)
        ON CONFLICT (stable_key) DO UPDATE SET stable_key = excluded.stable_key
        RETURNING id;
        """;

    /// <summary>Labels are stored per segment so history keeps the meaning it was recorded with.</summary>
    public const string InsertSegmentSensor = """
        INSERT INTO segment_sensor (segment_id, ordinal, sensor_id, name, kind, unit, role)
        VALUES (@SegmentId, @Ordinal, @SensorId, @Name, @Kind, @Unit, @Role);
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
        SELECT s.stable_key AS StableKey, ss.name AS Name, ss.kind AS Kind, ss.unit AS Unit, ss.role AS Role
        FROM segment_sensor ss JOIN sensor s ON s.id = ss.sensor_id
        WHERE ss.segment_id = @SegmentId
        ORDER BY ss.ordinal;
        """;

    public const string SelectFrames = """
        SELECT ts_ms AS TimestampMs, "values" AS "Values" FROM frame WHERE segment_id = @SegmentId ORDER BY ts_ms, id;
        """;
}

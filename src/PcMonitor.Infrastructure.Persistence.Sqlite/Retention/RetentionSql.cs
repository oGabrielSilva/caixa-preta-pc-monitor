namespace PcMonitor.Infrastructure.Persistence.Sqlite.Retention;

/// <summary>SQL for 1-minute aggregation and raw-frame deletion.</summary>
internal static class RetentionSql
{
    public const long MinuteMs = 60_000;

    /// <summary>Complete minutes (before the cutoff) that have frames but no aggregate yet.</summary>
    public const string PendingMinutes = """
        SELECT segment_id AS SegmentId, (ts_ms / 60000) * 60000 AS MinuteMs
        FROM frame WHERE ts_ms < @CutoffMs
        GROUP BY segment_id, MinuteMs
        EXCEPT
        SELECT segment_id, minute_ms FROM aggregate_1m
        ORDER BY SegmentId, MinuteMs;
        """;

    public const string SensorIdsByOrdinal = """
        SELECT sensor_id FROM segment_sensor WHERE segment_id = @SegmentId ORDER BY ordinal;
        """;

    public const string FramesOfMinute = """
        SELECT "values" FROM frame WHERE segment_id = @SegmentId AND ts_ms >= @MinuteMs AND ts_ms < @MinuteMs + 60000;
        """;

    public const string InsertAggregate = """
        INSERT INTO aggregate_1m (segment_id, minute_ms, sensor_id, min, avg, max)
        VALUES (@SegmentId, @MinuteMs, @SensorId, @Min, @Avg, @Max);
        """;

    public const string SensorIds = "SELECT id FROM sensor ORDER BY id;";

    /// <summary>
    /// Hourly summary of one sensor's minute aggregates, from the hour after its last summary up to the cutoff (a complete
    /// hour); reads through <c>ix_aggregate_sensor_minute</c>.
    /// </summary>
    public const string AggregateHours = """
        INSERT OR IGNORE INTO aggregate_1h (sensor_id, hour_ms, min, avg, max)
        SELECT sensor_id, minute_ms / 3600000 * 3600000 AS hour_ms, MIN(min), AVG(avg), MAX(max)
        FROM aggregate_1m
        WHERE sensor_id = @SensorId
          AND minute_ms >= (SELECT COALESCE(MAX(hour_ms) + 3600000, 0) FROM aggregate_1h WHERE sensor_id = @SensorId)
          AND minute_ms < @CutoffMs
          AND min IS NOT NULL
        GROUP BY hour_ms;
        """;

    /// <summary>One sensor's minute aggregates older than the cutoff whose hour already has its summary.</summary>
    public const string DeleteMinuteAggregates = """
        DELETE FROM aggregate_1m
        WHERE sensor_id = @SensorId
          AND minute_ms < MIN(@CutoffMs, (SELECT COALESCE(MAX(hour_ms) + 3600000, 0) FROM aggregate_1h WHERE sensor_id = @SensorId));
        """;

    /// <summary>
    /// Old frames, except those within the incident window before the end of an incident's telemetry, in any segment
    /// of the session that went down.
    /// </summary>
    public const string DeleteFramesBatch = """
        DELETE FROM frame WHERE id IN (
            SELECT f.id FROM frame f
            WHERE f.ts_ms < @CutoffMs
              AND NOT EXISTS (
                  SELECT 1 FROM incident i JOIN segment g ON g.session_id = i.session_id
                  WHERE g.id = f.segment_id
                    AND i.telemetry_end_ms IS NOT NULL
                    AND f.ts_ms BETWEEN i.telemetry_end_ms - @IncidentWindowMs AND i.telemetry_end_ms)
            LIMIT @BatchSize);
        """;
}

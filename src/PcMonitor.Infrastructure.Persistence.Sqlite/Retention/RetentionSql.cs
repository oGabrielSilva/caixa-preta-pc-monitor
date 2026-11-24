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

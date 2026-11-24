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

    /// <summary>Frames this long before an incident's estimated failure are never deleted.</summary>
    public const long IncidentWindowMs = 120_000;

    public const string DeleteFramesBatch = """
        DELETE FROM frame WHERE id IN (
            SELECT f.id FROM frame f
            WHERE f.ts_ms < @CutoffMs
              AND NOT EXISTS (
                  SELECT 1 FROM incident i
                  WHERE i.segment_id = f.segment_id
                    AND f.ts_ms BETWEEN i.estimated_failure_ms - @IncidentWindowMs AND i.estimated_failure_ms)
            LIMIT @BatchSize);
        """;
}

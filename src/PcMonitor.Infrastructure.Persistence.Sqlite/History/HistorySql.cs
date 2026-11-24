namespace PcMonitor.Infrastructure.Persistence.Sqlite.History;

/// <summary>SQL for the history view.</summary>
internal static class HistorySql
{
    private const string SensorSelect = """
        SELECT s.stable_key AS StableKey, ss.name AS Name, ss.kind AS Kind, ss.unit AS Unit, ss.role AS Role
        FROM segment_sensor ss JOIN sensor s ON s.id = ss.sensor_id
        WHERE ss.segment_id = (SELECT MAX(latest.segment_id) FROM segment_sensor latest WHERE latest.sensor_id = ss.sensor_id)
        """;

    /// <summary>One row per sensor, labelled as in the most recent segment that recorded it.</summary>
    public const string Sensors = SensorSelect + " ORDER BY s.id;";

    public const string Sensor = SensorSelect + " AND s.stable_key = @StableKey;";

    public const string Aggregates = """
        SELECT a.minute_ms AS MinuteMs, a.min AS Min, a.avg AS Avg, a.max AS Max
        FROM aggregate_1m a JOIN sensor s ON s.id = a.sensor_id
        WHERE s.stable_key = @StableKey AND a.minute_ms >= @SinceMs AND a.minute_ms < @UntilMs
          AND a.min IS NOT NULL;
        """;

    /// <summary>The newest aggregated minute of the sensor (through <c>ix_aggregate_sensor_minute</c>).</summary>
    public const string LastAggregatedMinute = """
        SELECT MAX(a.minute_ms) FROM aggregate_1m a JOIN sensor s ON s.id = a.sensor_id WHERE s.stable_key = @StableKey;
        """;

    /// <summary>
    /// Frames of minutes retention has not aggregated yet. Retention aggregates every complete minute when it runs, so
    /// the caller passes <c>@SinceMs</c> no earlier than the minute after <see cref="LastAggregatedMinute"/>; the
    /// <c>NOT EXISTS</c> keeps a minute from being counted twice.
    /// </summary>
    public const string PendingFrames = """
        SELECT f.ts_ms AS TimestampMs, f."values" AS "Values", ss.ordinal AS Ordinal
        FROM frame f
        JOIN segment_sensor ss ON ss.segment_id = f.segment_id
        JOIN sensor s ON s.id = ss.sensor_id AND s.stable_key = @StableKey
        WHERE f.ts_ms >= @SinceMs AND f.ts_ms < @UntilMs
          AND NOT EXISTS (SELECT 1 FROM aggregate_1m a WHERE a.segment_id = f.segment_id AND a.minute_ms = f.ts_ms / 60000 * 60000)
        ORDER BY f.ts_ms;
        """;
}

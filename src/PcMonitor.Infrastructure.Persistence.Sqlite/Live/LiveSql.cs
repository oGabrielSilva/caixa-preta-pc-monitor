namespace PcMonitor.Infrastructure.Persistence.Sqlite.Live;

/// <summary>SQL for what is being recorded now.</summary>
internal static class LiveSql
{
    public const string LatestSession = """
        SELECT id AS Id, started_at_ms AS StartedAtMs, source_status AS SourceStatus, board_product AS BoardProduct,
               ended_at_ms AS EndedAtMs, end_reason AS EndReason
        FROM session ORDER BY id DESC LIMIT 1;
        """;

    public const string LatestSegment = """
        SELECT id AS Id, last_frame_ms AS LastFrameMs FROM segment WHERE session_id = @SessionId ORDER BY id DESC LIMIT 1;
        """;

    public const string FramesSince = """
        SELECT ts_ms AS TimestampMs, "values" AS "Values" FROM frame
        WHERE segment_id = @SegmentId AND ts_ms >= @SinceMs
        ORDER BY ts_ms, id;
        """;

    public const string FramesAfter = """
        SELECT ts_ms AS TimestampMs, "values" AS "Values" FROM frame
        WHERE segment_id = @SegmentId AND ts_ms > @AfterMs
        ORDER BY ts_ms, id;
        """;
}

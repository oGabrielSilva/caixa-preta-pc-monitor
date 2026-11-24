using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Application.Live;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Infrastructure.Persistence.Sqlite.Frames;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Live;

/// <summary><see cref="ILiveReader"/> on SQLite, through read-only connections.</summary>
public sealed class SqliteLiveReader(SqliteDatabase database) : ILiveReader
{
    public LiveSnapshot? Latest(TimeSpan window)
    {
        using var connection = database.OpenReader();
        var (session, segment) = ReadHead(connection);
        if (session is null || segment is null)
        {
            return null;
        }

        var frames = segment.LastFrameMs is { } lastMs
            ? FrameRow.Read(connection, LiveSql.FramesSince, new { SegmentId = segment.Id, SinceMs = lastMs - (long)window.TotalMilliseconds })
            : [];
        return new LiveSnapshot(session.ToView(), segment.Id, SensorRow.Catalog(connection, segment.Id), frames);
    }

    public IReadOnlyList<Frame> FramesAfter(long segmentId, DateTimeOffset after)
    {
        using var connection = database.OpenReader();
        return FrameRow.Read(connection, LiveSql.FramesAfter, new { SegmentId = segmentId, AfterMs = after.ToUnixTimeMilliseconds() });
    }

    public LiveHead Head()
    {
        using var connection = database.OpenReader();
        var (session, segment) = ReadHead(connection);
        return new LiveHead(
            segment?.Id,
            session?.ToView(),
            segment?.LastFrameMs is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null);
    }

    private static (SessionRow? Session, SegmentRow? Segment) ReadHead(SqliteConnection connection)
    {
        var session = connection.QuerySingleOrDefault<SessionRow>(LiveSql.LatestSession);
        var segment = session is null ? null : connection.QuerySingleOrDefault<SegmentRow>(LiveSql.LatestSegment, new { SessionId = session.Id });
        return (session, segment);
    }

    // Settable-property rows: Dapper cannot match positional constructors when SQLite reports NULL columns.
    private sealed class SessionRow
    {
        public long Id { get; init; }

        public long StartedAtMs { get; init; }

        public string SourceStatus { get; init; } = string.Empty;

        public string? BoardProduct { get; init; }

        public long? EndedAtMs { get; init; }

        public string? EndReason { get; init; }

        public SessionView ToView() => new(
            Id,
            DateTimeOffset.FromUnixTimeMilliseconds(StartedAtMs),
            Enum.Parse<SensorSourceStatus>(SourceStatus),
            BoardProduct,
            EndedAtMs is { } ended ? DateTimeOffset.FromUnixTimeMilliseconds(ended) : null,
            EndReason is null ? null : Enum.Parse<EndReason>(EndReason));
    }

    private sealed class SegmentRow
    {
        public long Id { get; init; }

        public long? LastFrameMs { get; init; }
    }
}

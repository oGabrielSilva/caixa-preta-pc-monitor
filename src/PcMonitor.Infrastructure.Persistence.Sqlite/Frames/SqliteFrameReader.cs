using Dapper;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Frames;

/// <summary>Reads segments back through a read-only connection.</summary>
public sealed class SqliteFrameReader(SqliteDatabase database)
{
    /// <summary>Sensors of a segment, in the order of the values in its frames.</summary>
    public SensorCatalog CatalogOf(long segmentId)
    {
        using var connection = database.OpenReader();
        var rows = connection.Query<SensorRow>(MonitoringSql.SelectCatalog, new { SegmentId = segmentId });
        return new SensorCatalog(rows.Select(row => row.ToDescriptor()));
    }

    /// <summary>Every frame of a segment, ordered by time.</summary>
    public IReadOnlyList<Frame> ReadSegment(long segmentId)
    {
        using var connection = database.OpenReader();
        return [.. connection.Query<FrameRow>(MonitoringSql.SelectFrames, new { SegmentId = segmentId })
            .Select(row => new Frame(DateTimeOffset.FromUnixTimeMilliseconds(row.TimestampMs), FrameCodec.Decode(row.Values)))];
    }

    private sealed record SensorRow(string StableKey, string Name, string Kind, string Unit, string Role)
    {
        public SensorDescriptor ToDescriptor() =>
            new(StableKey, Name, Enum.Parse<SensorKind>(Kind), Unit, Enum.Parse<SensorRole>(Role));
    }

    private sealed record FrameRow(long TimestampMs, byte[] Values);
}

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
        return SensorRow.Catalog(connection, segmentId);
    }

    /// <summary>Every frame of a segment, ordered by time.</summary>
    public IReadOnlyList<Frame> ReadSegment(long segmentId)
    {
        using var connection = database.OpenReader();
        return FrameRow.Read(connection, MonitoringSql.SelectFrames, new { SegmentId = segmentId });
    }
}

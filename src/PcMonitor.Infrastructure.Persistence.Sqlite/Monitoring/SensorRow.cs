using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;

/// <summary>A sensor as stored for one segment (labels are kept per segment).</summary>
internal sealed record SensorRow(string StableKey, string Name, string Kind, string Unit, string Role)
{
    public SensorDescriptor ToDescriptor() =>
        new(StableKey, Name, Enum.Parse<SensorKind>(Kind), Unit, Enum.Parse<SensorRole>(Role));

    /// <summary>The sensors of a segment, in the order of the values in its frames.</summary>
    public static SensorCatalog Catalog(SqliteConnection connection, long segmentId) =>
        new(connection.Query<SensorRow>(MonitoringSql.SelectCatalog, new { SegmentId = segmentId }).Select(row => row.ToDescriptor()));
}

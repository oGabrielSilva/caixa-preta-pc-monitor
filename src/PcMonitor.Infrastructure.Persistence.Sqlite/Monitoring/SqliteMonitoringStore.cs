using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Frames;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;

/// <summary>
/// <see cref="IMonitoringStore"/> on SQLite. Keeps one writer connection open for the life of the collector;
/// every call is its own committed transaction.
/// </summary>
public sealed class SqliteMonitoringStore(SqliteDatabase database) : IMonitoringStore, IDisposable
{
    private SqliteConnection? _connection;

    private SqliteConnection Connection => _connection ??= database.OpenWriter();

    public long StartSession(DateTimeOffset startedAt, DateTimeOffset bootedAt, SensorSourceInfo source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Connection.ExecuteScalar<long>(MonitoringSql.InsertSession, new
        {
            StartedAtMs = startedAt.ToUnixTimeMilliseconds(),
            BootedAtMs = bootedAt.ToUnixTimeMilliseconds(),
            SourceStatus = source.Status.ToString(),
            source.BoardProduct,
        });
    }

    public long StartSegment(long sessionId, DateTimeOffset startedAt, SensorCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        using var transaction = Connection.BeginTransaction();

        var segmentId = Connection.ExecuteScalar<long>(
            MonitoringSql.InsertSegment, new { SessionId = sessionId, StartedAtMs = startedAt.ToUnixTimeMilliseconds() }, transaction);

        for (var ordinal = 0; ordinal < catalog.Count; ordinal++)
        {
            var sensor = catalog[ordinal];
            var sensorId = Connection.ExecuteScalar<long>(MonitoringSql.UpsertSensor, new
            {
                sensor.StableKey,
                sensor.Name,
                Kind = sensor.Kind.ToString(),
                sensor.Unit,
                Role = sensor.Role.ToString(),
            }, transaction);
            Connection.Execute(
                MonitoringSql.InsertSegmentSensor, new { SegmentId = segmentId, Ordinal = ordinal, SensorId = sensorId }, transaction);
        }

        transaction.Commit();
        return segmentId;
    }

    public void AppendFrame(long segmentId, Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        using var transaction = Connection.BeginTransaction();
        Connection.Execute(MonitoringSql.InsertFrame, new
        {
            SegmentId = segmentId,
            TimestampMs = frame.Timestamp.ToUnixTimeMilliseconds(),
            Values = FrameCodec.Encode(frame.Values),
        }, transaction);
        transaction.Commit();
    }

    public void EndSegment(long segmentId, DateTimeOffset endedAt, EndReason reason) =>
        Connection.Execute(MonitoringSql.EndSegment, new { Id = segmentId, EndedAtMs = endedAt.ToUnixTimeMilliseconds(), Reason = reason.ToString() });

    public void EndSession(long sessionId, DateTimeOffset endedAt, EndReason reason) =>
        Connection.Execute(MonitoringSql.EndSession, new { Id = sessionId, EndedAtMs = endedAt.ToUnixTimeMilliseconds(), Reason = reason.ToString() });

    public void Dispose() => _connection?.Dispose();
}

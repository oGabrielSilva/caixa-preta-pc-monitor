using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Application.Retention;
using PcMonitor.Infrastructure.Persistence.Sqlite.Frames;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Retention;

/// <summary>
/// <see cref="IRetentionStore"/> on SQLite. Works in small transactions so the sampler, which writes on another
/// connection every second, never waits long for the write lock.
/// </summary>
public sealed class SqliteRetentionStore(SqliteDatabase database) : IRetentionStore
{
    private const int MinutesPerTransaction = 60;
    private const int DeleteBatchSize = 10_000;

    public int AggregateMinutesBefore(DateTimeOffset cutoff)
    {
        using var connection = database.OpenWriter();
        var pending = connection.Query<PendingMinute>(RetentionSql.PendingMinutes, new { CutoffMs = cutoff.ToUnixTimeMilliseconds() })
            .ToList();

        var sensorIdsBySegment = new Dictionary<long, long[]>();
        foreach (var chunk in pending.Chunk(MinutesPerTransaction))
        {
            using var transaction = connection.BeginTransaction();
            foreach (var minute in chunk)
            {
                if (!sensorIdsBySegment.TryGetValue(minute.SegmentId, out var sensorIds))
                {
                    sensorIds = [.. connection.Query<long>(RetentionSql.SensorIdsByOrdinal, new { minute.SegmentId }, transaction)];
                    sensorIdsBySegment[minute.SegmentId] = sensorIds;
                }

                AggregateMinute(connection, transaction, minute, sensorIds);
            }

            transaction.Commit();
        }

        return pending.Count;
    }

    public int AggregateHoursBefore(DateTimeOffset cutoff) => PerSensor(RetentionSql.AggregateHours, cutoff);

    public int DeleteMinuteAggregatesBefore(DateTimeOffset cutoff) => PerSensor(RetentionSql.DeleteMinuteAggregates, cutoff);

    /// <summary>Runs a per-sensor statement for every sensor (one short transaction each, through the sensor index).</summary>
    private int PerSensor(string sql, DateTimeOffset cutoff)
    {
        using var connection = database.OpenWriter();
        var cutoffMs = cutoff.ToUnixTimeMilliseconds();
        return connection.Query<long>(RetentionSql.SensorIds).ToList()
            .Sum(sensorId => connection.Execute(sql, new { SensorId = sensorId, CutoffMs = cutoffMs }));
    }

    public int DeleteFramesBefore(DateTimeOffset cutoff, TimeSpan incidentWindow)
    {
        using var connection = database.OpenWriter();
        var total = 0;
        int deleted;
        do
        {
            deleted = connection.Execute(
                RetentionSql.DeleteFramesBatch,
                new
                {
                    CutoffMs = cutoff.ToUnixTimeMilliseconds(),
                    IncidentWindowMs = (long)incidentWindow.TotalMilliseconds,
                    BatchSize = DeleteBatchSize,
                });
            total += deleted;
        }
        while (deleted == DeleteBatchSize);

        return total;
    }

    private static void AggregateMinute(SqliteConnection connection, SqliteTransaction transaction, PendingMinute minute, long[] sensorIds)
    {
        var frames = connection.Query<byte[]>(RetentionSql.FramesOfMinute, new { minute.SegmentId, minute.MinuteMs }, transaction)
            .Select(blob => FrameCodec.Decode(blob))
            .ToList();

        for (var ordinal = 0; ordinal < sensorIds.Length; ordinal++)
        {
            var values = frames.Where(f => ordinal < f.Length && float.IsFinite(f[ordinal])).Select(f => (double)f[ordinal]).ToList();
            if (values.Count == 0)
            {
                continue;
            }

            connection.Execute(RetentionSql.InsertAggregate, new
            {
                minute.SegmentId,
                minute.MinuteMs,
                SensorId = sensorIds[ordinal],
                Min = values.Min(),
                Avg = values.Average(),
                Max = values.Max(),
            }, transaction);
        }
    }

    // A class with settable properties, not a positional record: when the EXCEPT query returns no rows SQLite
    // reports computed columns as BLOB and Dapper rejects a constructor whose parameter types do not match.
    private sealed class PendingMinute
    {
        public long SegmentId { get; init; }

        public long MinuteMs { get; init; }
    }
}

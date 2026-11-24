using Dapper;
using PcMonitor.Application.History;
using PcMonitor.Infrastructure.Persistence.Sqlite.Frames;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.History;

/// <summary>
/// <see cref="IHistoryReader"/> on SQLite, through read-only connections. Minutes come from <c>aggregate_1m</c>; the
/// recent ones retention has not aggregated yet are computed from the frames, at the sensor's ordinal in each segment.
/// </summary>
public sealed class SqliteHistoryReader(SqliteDatabase database) : IHistoryReader
{
    private const long MinuteMs = 60_000;

    public IReadOnlyList<HistorySensor> Sensors()
    {
        using var connection = database.OpenReader();
        return [.. connection.Query<SensorRow>(HistorySql.Sensors).Select(row =>
        {
            var sensor = row.ToDescriptor();
            return new HistorySensor(sensor.StableKey, sensor.Name, sensor.Kind, sensor.Unit, sensor.Role);
        })];
    }

    public IReadOnlyList<MinuteAggregate> Minutes(string stableKey, DateTimeOffset since, DateTimeOffset until)
    {
        using var connection = database.OpenReader();
        var parameters = new { StableKey = stableKey, SinceMs = since.ToUnixTimeMilliseconds(), UntilMs = until.ToUnixTimeMilliseconds() };

        var stored = connection.Query<AggregateRow>(HistorySql.Aggregates, parameters)
            .Select(a => (a.MinuteMs, a.Min, a.Avg, a.Max));
        var computed = connection.Query<PendingFrameRow>(HistorySql.PendingFrames, parameters)
            .Select(f => (MinuteMs: f.TimestampMs / MinuteMs * MinuteMs, Value: ValueAt(f)))
            .Where(f => f.Value is { } value && !double.IsNaN(value))
            .GroupBy(f => f.MinuteMs)
            .Select(g => (MinuteMs: g.Key, Min: g.Min(f => f.Value!.Value), Avg: g.Average(f => f.Value!.Value), Max: g.Max(f => f.Value!.Value)));

        // A segment change inside a minute leaves one row per segment: merge them.
        return [.. stored.Concat(computed)
            .GroupBy(m => m.MinuteMs)
            .OrderBy(g => g.Key)
            .Select(g => new MinuteAggregate(DateTimeOffset.FromUnixTimeMilliseconds(g.Key), g.Min(m => m.Min), g.Average(m => m.Avg), g.Max(m => m.Max)))];
    }

    private static double? ValueAt(PendingFrameRow row)
    {
        var values = FrameCodec.Decode(row.Values);
        return row.Ordinal < values.Length ? values[(int)row.Ordinal] : null;
    }

    // Settable-property rows: Dapper cannot match positional constructors when SQLite reports computed columns.
    private sealed class AggregateRow
    {
        public long MinuteMs { get; init; }

        public double Min { get; init; }

        public double Avg { get; init; }

        public double Max { get; init; }
    }

    private sealed class PendingFrameRow
    {
        public long TimestampMs { get; init; }

        public byte[] Values { get; init; } = [];

        public long Ordinal { get; init; }
    }
}

using Dapper;
using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;
using PcMonitor.Infrastructure.Persistence.Sqlite.Retention;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SqliteRetentionStoreTests : IDisposable
{
    private static readonly DateTimeOffset _minute = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly SensorCatalog _catalog = new(
    [
        new SensorDescriptor("/a", "a", SensorKind.Voltage, "V", SensorRole.None),
        new SensorDescriptor("/b", "b", SensorKind.Voltage, "V", SensorRole.None),
    ]);

    private readonly TempDatabase _temp = TempDatabase.Migrated();
    private readonly SqliteMonitoringStore _store;
    private readonly SqliteRetentionStore _retention;
    private readonly long _session;
    private readonly long _segment;

    public SqliteRetentionStoreTests()
    {
        _store = new SqliteMonitoringStore(_temp.Database);
        _retention = new SqliteRetentionStore(_temp.Database);
        _session = _store.StartSession(_minute, _minute, new SensorSourceInfo(SensorSourceStatus.Ok, null, _catalog));
        _segment = _store.StartSegment(_session, _minute, _catalog);
    }

    public void Dispose()
    {
        _store.Dispose();
        _temp.Dispose();
    }

    private void Frame(DateTimeOffset at, float a, float b) => _store.AppendFrame(_segment, new Frame(at, [a, b]));

    private List<(string Key, long Minute, double? Min, double? Avg, double? Max)> Aggregates()
    {
        using var connection = _temp.Database.OpenReader();
        return [.. connection.Query<(string, long, double?, double?, double?)>("""
            SELECT s.stable_key, a.minute_ms, a.min, a.avg, a.max
            FROM aggregate_1m a JOIN sensor s ON s.id = a.sensor_id ORDER BY a.minute_ms, s.stable_key
            """)];
    }

    [Fact]
    public void Aggregation_computes_min_avg_max_per_sensor_and_minute_ignoring_NaN()
    {
        Frame(_minute.AddSeconds(1), 1f, float.NaN);
        Frame(_minute.AddSeconds(2), 3f, float.NaN);
        Frame(_minute.AddSeconds(3), float.NaN, float.NaN);

        _retention.AggregateMinutesBefore(_minute.AddMinutes(1)).ShouldBe(1);

        Aggregates().ShouldBe([("/a", _minute.ToUnixTimeMilliseconds(), 1d, 2d, 3d)]);
    }

    [Fact]
    public void Aggregation_skips_the_current_incomplete_minute()
    {
        Frame(_minute.AddSeconds(1), 1f, 1f);
        Frame(_minute.AddMinutes(1).AddSeconds(1), 2f, 2f);

        _retention.AggregateMinutesBefore(_minute.AddMinutes(1)).ShouldBe(1);

        Aggregates().Select(a => a.Minute).Distinct().ShouldBe([_minute.ToUnixTimeMilliseconds()]);
    }

    [Fact]
    public void Aggregation_does_not_duplicate_minutes_already_aggregated()
    {
        Frame(_minute.AddSeconds(1), 1f, 1f);
        _retention.AggregateMinutesBefore(_minute.AddMinutes(1));

        _retention.AggregateMinutesBefore(_minute.AddMinutes(1)).ShouldBe(0);

        Aggregates().Count.ShouldBe(2);
    }

    [Fact]
    public void Retention_keeps_the_120_seconds_before_an_incident_across_segments_of_the_session()
    {
        // The machine stuttered 60 s before dying: a Gap split the session, so the window spans two segments.
        for (var second = 0; second < 180; second += 30)
        {
            Frame(_minute.AddSeconds(second), second, second);
        }

        _store.EndSegment(_segment, _minute.AddSeconds(150), EndReason.Gap);
        var after = _store.StartSegment(_session, _minute.AddSeconds(200), _catalog);
        _store.AppendFrame(after, new Frame(_minute.AddSeconds(210), [1f, 1f]));
        _store.AppendFrame(after, new Frame(_minute.AddSeconds(240), [1f, 1f]));

        new Incidents.SqliteIncidentStore(_temp.Database).SaveIncident(new PcMonitor.Domain.Incidents.Incident(
            SqliteIncidentStoreTests.Crash(1, _minute.AddMinutes(10)), _minute.AddSeconds(240),
            PcMonitor.Domain.Incidents.FailureTimeSource.LastFrame, _session, after, _minute.AddSeconds(240), []), closure: null);

        _retention.DeleteFramesBefore(_minute.AddHours(1), TimeSpan.FromSeconds(120));

        using var connection = _temp.Database.OpenReader();
        connection.Query<long>("SELECT ts_ms FROM frame ORDER BY ts_ms")
            .Select(ms => (ms - _minute.ToUnixTimeMilliseconds()) / 1000)
            .ShouldBe([120L, 150L, 210L, 240L]);
    }

    [Fact]
    public void DeleteFramesBefore_removes_only_older_frames()
    {
        Frame(_minute, 1f, 1f);
        Frame(_minute.AddSeconds(30), 2f, 2f);

        _retention.DeleteFramesBefore(_minute.AddSeconds(30), TimeSpan.FromSeconds(120)).ShouldBe(1);

        using var connection = _temp.Database.OpenReader();
        connection.ExecuteScalar<long>("SELECT COUNT(*) FROM frame").ShouldBe(1);
    }

    private List<(string Key, long Hour, double Min, double Avg, double Max)> Hours()
    {
        using var connection = _temp.Database.OpenReader();
        return [.. connection.Query<(string, long, double, double, double)>("""
            SELECT s.stable_key, h.hour_ms, h.min, h.avg, h.max
            FROM aggregate_1h h JOIN sensor s ON s.id = h.sensor_id ORDER BY h.hour_ms, s.stable_key
            """)];
    }

    /// <summary>One frame at the start of each minute for <paramref name="minutes"/> minutes: sensor a = minute index, b = 10.</summary>
    private void FramesEveryMinute(int minutes)
    {
        for (var minute = 0; minute < minutes; minute++)
        {
            Frame(_minute.AddMinutes(minute), minute, 10f);
        }

        _retention.AggregateMinutesBefore(_minute.AddMinutes(minutes));
    }

    [Fact]
    public void Hours_summarise_the_minute_aggregates_of_complete_hours()
    {
        FramesEveryMinute(125);

        _retention.AggregateHoursBefore(_minute.AddHours(2)).ShouldBe(4);

        Hours().ShouldBe(
        [
            ("/a", _minute.ToUnixTimeMilliseconds(), 0d, 29.5d, 59d),
            ("/b", _minute.ToUnixTimeMilliseconds(), 10d, 10d, 10d),
            ("/a", _minute.AddHours(1).ToUnixTimeMilliseconds(), 60d, 89.5d, 119d),
            ("/b", _minute.AddHours(1).ToUnixTimeMilliseconds(), 10d, 10d, 10d),
        ]);
    }

    [Fact]
    public void Hours_already_summarised_are_not_redone()
    {
        FramesEveryMinute(125);
        _retention.AggregateHoursBefore(_minute.AddHours(1));

        _retention.AggregateHoursBefore(_minute.AddHours(2)).ShouldBe(2);
        _retention.AggregateHoursBefore(_minute.AddHours(2)).ShouldBe(0);
    }

    [Fact]
    public void Minute_aggregates_are_deleted_only_once_their_hour_is_summarised()
    {
        FramesEveryMinute(125);
        _retention.AggregateHoursBefore(_minute.AddHours(1));

        // Cutoff after both hours, but only the first one has its hourly summary.
        _retention.DeleteMinuteAggregatesBefore(_minute.AddHours(2)).ShouldBe(120);

        Aggregates().Select(a => a.Minute).Distinct().Count().ShouldBe(65);
        Aggregates().Min(a => a.Minute).ShouldBe(_minute.AddHours(1).ToUnixTimeMilliseconds());
    }

    [Theory]
    [InlineData(nameof(RetentionSql.AggregateHours))]
    [InlineData(nameof(RetentionSql.DeleteMinuteAggregates))]
    public void Hourly_retention_reads_minute_aggregates_through_an_index(string query)
    {
        var sql = (string)typeof(RetentionSql).GetField(query)!.GetValue(null)!;
        using var connection = _temp.Database.OpenReader();

        var plan = connection.Query<(long Id, long Parent, long NotUsed, string Detail)>(
            $"EXPLAIN QUERY PLAN {sql}", new { SensorId = 1L, CutoffMs = 0L }).Select(r => r.Detail).ToList();

        plan.Where(detail => detail.Contains("aggregate_1m", StringComparison.Ordinal))
            .ShouldAllBe(detail => detail.StartsWith("SEARCH", StringComparison.Ordinal) && detail.Contains("?)", StringComparison.Ordinal));
    }
}

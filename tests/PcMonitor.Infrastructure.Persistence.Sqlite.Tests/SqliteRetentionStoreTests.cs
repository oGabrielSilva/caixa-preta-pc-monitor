using Dapper;
using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;
using PcMonitor.Infrastructure.Persistence.Sqlite.Retention;
using PcMonitor.Infrastructure.Persistence.Sqlite.Tests.Support;

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
    private readonly long _segment;

    public SqliteRetentionStoreTests()
    {
        _store = new SqliteMonitoringStore(_temp.Database);
        _retention = new SqliteRetentionStore(_temp.Database);
        var session = _store.StartSession(_minute, _minute, new SensorSourceInfo(SensorSourceStatus.Ok, null, _catalog));
        _segment = _store.StartSegment(session, _minute, _catalog);
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
    public void Retention_keeps_the_120_seconds_before_an_incident()
    {
        for (var second = 0; second < 300; second += 30)
        {
            Frame(_minute.AddSeconds(second), second, second);
        }

        var failure = _minute.AddSeconds(240);
        new Incidents.SqliteIncidentStore(_temp.Database).SaveIncident(new PcMonitor.Domain.Incidents.Incident(
            SqliteIncidentStoreTests.Crash(1, failure.AddMinutes(3)), failure, PcMonitor.Domain.Incidents.FailureTimeSource.LastFrame,
            null, _segment, []));

        _retention.DeleteFramesBefore(_minute.AddHours(1));

        using var connection = _temp.Database.OpenReader();
        connection.Query<long>("SELECT ts_ms FROM frame ORDER BY ts_ms")
            .Select(ms => (ms - _minute.ToUnixTimeMilliseconds()) / 1000)
            .ShouldBe([120L, 150L, 180L, 210L, 240L]);
    }

    [Fact]
    public void DeleteFramesBefore_removes_only_older_frames()
    {
        Frame(_minute, 1f, 1f);
        Frame(_minute.AddSeconds(30), 2f, 2f);

        _retention.DeleteFramesBefore(_minute.AddSeconds(30)).ShouldBe(1);

        using var connection = _temp.Database.OpenReader();
        connection.ExecuteScalar<long>("SELECT COUNT(*) FROM frame").ShouldBe(1);
    }
}

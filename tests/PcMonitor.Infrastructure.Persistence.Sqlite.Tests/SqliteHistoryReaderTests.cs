using Dapper;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.History;
using PcMonitor.Infrastructure.Persistence.Sqlite.Retention;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SqliteHistoryReaderTests : IDisposable
{
    private const string Rail12V = "/lpc/nct6799d/0/voltage/4";
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _temp = TempDatabase.Migrated();
    private readonly DatabaseSeed _seed;
    private readonly SqliteHistoryReader _reader;

    public SqliteHistoryReaderTests()
    {
        _seed = new DatabaseSeed(_temp.Database);
        _reader = new SqliteHistoryReader(_temp.Database);
    }

    public void Dispose()
    {
        _seed.Dispose();
        _temp.Dispose();
    }

    private static float[] Rail(float volts) => [45f, 6f, 20f, volts, 5f, 3.3f, 2000f];

    [Fact]
    public void Sensors_are_distinct_by_stable_key_with_the_latest_name()
    {
        var session = _seed.StartSession(_t0);
        _seed.StartSegment(session, _t0);
        var renamed = new SensorCatalog([new SensorDescriptor(Rail12V, "+12 V rail", SensorKind.Voltage, "V", SensorRole.Rail12V)]);
        _seed.StartSegment(session, _t0.AddHours(1), renamed);

        var sensors = _reader.Sensors();

        sensors.Count.ShouldBe(DatabaseSeed.Catalog.Count);
        sensors.Single(s => s.StableKey == Rail12V).ShouldBe(new Application.History.HistorySensor(Rail12V, "+12 V rail", SensorKind.Voltage, "V", SensorRole.Rail12V));
    }

    [Fact]
    public void Minutes_read_aggregates()
    {
        var segment = _seed.StartSegment(_seed.StartSession(_t0), _t0);
        _seed.Frames(segment, _t0, 180, s => Rail(12.0f + (s / 60 * 0.1f)));
        new SqliteRetentionStore(_temp.Database).AggregateMinutesBefore(_t0.AddMinutes(3));
        using (var connection = _temp.Database.OpenWriter())
        {
            connection.Execute("DELETE FROM frame"); // only aggregates remain
        }

        var minutes = _reader.Minutes(Rail12V, _t0, _t0.AddHours(1));

        minutes.Select(m => m.Minute).ShouldBe([_t0, _t0.AddMinutes(1), _t0.AddMinutes(2)]);
        minutes[1].Avg.ShouldBe(12.1, tolerance: 0.001);
    }

    [Fact]
    public void Minutes_without_aggregate_are_computed_from_frames()
    {
        var segment = _seed.StartSegment(_seed.StartSession(_t0), _t0);
        _seed.Frames(segment, _t0, 300, s => Rail(s < 180 ? 12.19f : (s % 2 == 0 ? 11.9f : 12.1f)));
        new SqliteRetentionStore(_temp.Database).AggregateMinutesBefore(_t0.AddMinutes(3));

        var minutes = _reader.Minutes(Rail12V, _t0, _t0.AddHours(1));

        minutes.Count.ShouldBe(5);
        minutes[4].Minute.ShouldBe(_t0.AddMinutes(4));
        minutes[4].Min.ShouldBe(11.9, tolerance: 0.001);
        minutes[4].Max.ShouldBe(12.1, tolerance: 0.001);
        minutes[4].Avg.ShouldBe(12.0, tolerance: 0.001);
    }

    [Fact]
    public void NaN_values_are_ignored_in_computed_minutes()
    {
        var segment = _seed.StartSegment(_seed.StartSession(_t0), _t0);
        _seed.Frames(segment, _t0, 60, s => Rail(s % 2 == 0 ? float.NaN : 12.2f));

        var minute = _reader.Minutes(Rail12V, _t0, _t0.AddHours(1)).ShouldHaveSingleItem();

        minute.Min.ShouldBe(12.2, tolerance: 0.001);
        minute.Max.ShouldBe(12.2, tolerance: 0.001);
    }

    [Fact]
    public void Minutes_follow_the_sensor_ordinal_of_each_segment()
    {
        var session = _seed.StartSession(_t0);
        var first = _seed.StartSegment(session, _t0);
        _seed.Frames(first, _t0, 60, _ => Rail(12.2f));
        var reordered = new SensorCatalog([new SensorDescriptor(Rail12V, "+12V", SensorKind.Voltage, "V", SensorRole.Rail12V)]);
        var second = _seed.StartSegment(session, _t0.AddMinutes(1), reordered);
        _seed.Frames(second, _t0.AddMinutes(1), 60, _ => [11.8f]);

        _reader.Minutes(Rail12V, _t0, _t0.AddHours(1)).Select(m => Math.Round(m.Avg, 2)).ShouldBe([12.2, 11.8]);
    }

    [Fact]
    public void Minutes_outside_the_period_are_excluded()
    {
        var segment = _seed.StartSegment(_seed.StartSession(_t0), _t0);
        _seed.Frames(segment, _t0, 180);

        _reader.Minutes(Rail12V, _t0.AddMinutes(1), _t0.AddMinutes(2)).Select(m => m.Minute).ShouldBe([_t0.AddMinutes(1)]);
    }

    [Fact]
    public void Unknown_sensor_has_no_minutes() => _reader.Minutes("/unknown", _t0, _t0.AddHours(1)).ShouldBeEmpty();
}

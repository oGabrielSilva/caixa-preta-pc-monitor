using Dapper;
using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Frames;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SqliteMonitoringStoreTests : IDisposable
{
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _temp = TempDatabase.Migrated();
    private readonly SqliteMonitoringStore _store;
    private readonly SqliteFrameReader _reader;

    public SqliteMonitoringStoreTests()
    {
        _store = new SqliteMonitoringStore(_temp.Database);
        _reader = new SqliteFrameReader(_temp.Database);
    }

    public void Dispose()
    {
        _store.Dispose();
        _temp.Dispose();
    }

    private static SensorDescriptor Sensor(string key, SensorRole role = SensorRole.None) =>
        new(key, key.TrimStart('/'), SensorKind.Voltage, "V", role);

    private static SensorCatalog Catalog(params string[] keys) => new(keys.Select(k => Sensor(k)));

    private long NewSegment(SensorCatalog catalog)
    {
        var session = _store.StartSession(_t0, _t0.AddMinutes(-5), new SensorSourceInfo(SensorSourceStatus.Ok, "BOARD", catalog));
        return _store.StartSegment(session, _t0, catalog);
    }

    [Fact]
    public void Store_writes_frames_that_read_back_in_order()
    {
        var segment = NewSegment(Catalog("/a", "/b"));

        _store.AppendFrame(segment, new Frame(_t0.AddSeconds(1), [1f, 2f]));
        _store.AppendFrame(segment, new Frame(_t0.AddSeconds(2), [3f, 4f]));

        var frames = _reader.ReadSegment(segment);
        frames.Select(f => f.Timestamp).ShouldBe([_t0.AddSeconds(1), _t0.AddSeconds(2)]);
        frames.Select(f => f.Values.ToArray()).ShouldBe([[1f, 2f], [3f, 4f]]);
    }

    [Fact]
    public void Store_keeps_two_frames_with_the_same_timestamp()
    {
        var segment = NewSegment(Catalog("/a"));

        _store.AppendFrame(segment, new Frame(_t0, [1f]));
        _store.AppendFrame(segment, new Frame(_t0, [2f]));

        _reader.ReadSegment(segment).Select(f => f.Values[0]).ShouldBe([1f, 2f]);
    }

    [Fact]
    public void Store_keeps_a_frame_whose_timestamp_goes_back_in_time()
    {
        var segment = NewSegment(Catalog("/a"));

        _store.AppendFrame(segment, new Frame(_t0.AddSeconds(10), [1f]));
        _store.AppendFrame(segment, new Frame(_t0.AddSeconds(5), [2f]));

        _reader.ReadSegment(segment).Count.ShouldBe(2);
    }

    [Fact]
    public void Each_segment_decodes_with_its_own_catalog()
    {
        var first = NewSegment(Catalog("/a", "/b"));
        var second = NewSegment(Catalog("/b", "/c"));
        _store.AppendFrame(first, new Frame(_t0, [1f, 2f]));
        _store.AppendFrame(second, new Frame(_t0, [20f, 30f]));

        Value(second, "/b").ShouldBe(20f);
        Value(second, "/c").ShouldBe(30f);
        Value(first, "/b").ShouldBe(2f);

        float Value(long segment, string key)
        {
            var catalog = _reader.CatalogOf(segment);
            var ordinal = catalog.Sensors.Select(s => s.StableKey).ToList().IndexOf(key);
            return _reader.ReadSegment(segment).Single().Values[ordinal];
        }
    }

    [Fact]
    public void Catalog_reads_back_names_kinds_units_and_roles()
    {
        var catalog = new SensorCatalog([Sensor("/a"), Sensor("/v12", SensorRole.Rail12V)]);
        var segment = NewSegment(catalog);

        _reader.CatalogOf(segment).Sensors.ShouldBe(catalog.Sensors);
    }

    [Fact]
    public void Relabelling_a_sensor_in_a_later_segment_does_not_rewrite_earlier_segments()
    {
        // A profile fix moves the +5V role from voltage/1 to voltage/5.
        var before = new SensorCatalog(
        [
            new SensorDescriptor("/v1", "+5V", SensorKind.Voltage, "V", SensorRole.Rail5V),
            new SensorDescriptor("/v5", "Voltage #6", SensorKind.Voltage, "V", SensorRole.None),
        ]);
        var after = new SensorCatalog(
        [
            new SensorDescriptor("/v1", "Voltage #2", SensorKind.Voltage, "V", SensorRole.None),
            new SensorDescriptor("/v5", "+5V", SensorKind.Voltage, "V", SensorRole.Rail5V),
        ]);
        var first = NewSegment(before);
        var second = NewSegment(after);

        _reader.CatalogOf(first).Sensors.ShouldBe(before.Sensors);
        _reader.CatalogOf(second).Sensors.ShouldBe(after.Sensors);
    }

    [Fact]
    public void Same_stable_key_maps_to_one_sensor_row_across_segments()
    {
        NewSegment(Catalog("/a", "/b"));
        NewSegment(Catalog("/b", "/c"));

        using var connection = _temp.Database.OpenReader();
        connection.Query<string>("SELECT stable_key FROM sensor ORDER BY stable_key").ShouldBe(["/a", "/b", "/c"]);
    }

    [Fact]
    public void AppendFrame_updates_the_segment_last_frame_time()
    {
        var segment = NewSegment(Catalog("/a"));

        _store.AppendFrame(segment, new Frame(_t0.AddSeconds(7), [1f]));

        using var connection = _temp.Database.OpenReader();
        connection.ExecuteScalar<long>("SELECT last_frame_ms FROM segment WHERE id = @segment", new { segment })
            .ShouldBe(_t0.AddSeconds(7).ToUnixTimeMilliseconds());
    }

    [Fact]
    public void AppendFrame_gives_up_quickly_when_another_connection_holds_the_write_lock()
    {
        var segment = NewSegment(Catalog("/a"));
        using var blocker = _temp.Database.OpenWriter();
        blocker.Execute("BEGIN IMMEDIATE");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        Should.Throw<Microsoft.Data.Sqlite.SqliteException>(() => _store.AppendFrame(segment, new Frame(_t0, [1f])));

        // Well inside the collector's 10 s stall timeout, so a busy database costs a frame, not a restart.
        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(4));
        blocker.Execute("ROLLBACK");
    }

    [Fact]
    public void EndSegment_and_EndSession_store_time_and_reason()
    {
        var catalog = Catalog("/a");
        var session = _store.StartSession(_t0, _t0, new SensorSourceInfo(SensorSourceStatus.DriverMissing, null, catalog));
        var segment = _store.StartSegment(session, _t0, catalog);

        _store.EndSegment(segment, _t0.AddMinutes(1), EndReason.Suspended);
        _store.EndSession(session, _t0.AddMinutes(2), EndReason.SystemShutdown);

        using var connection = _temp.Database.OpenReader();
        connection.QuerySingle<(long, string)>("SELECT ended_at_ms, end_reason FROM segment")
            .ShouldBe((_t0.AddMinutes(1).ToUnixTimeMilliseconds(), "Suspended"));
        connection.QuerySingle<(long, string, string)>("SELECT ended_at_ms, end_reason, source_status FROM session")
            .ShouldBe((_t0.AddMinutes(2).ToUnixTimeMilliseconds(), "SystemShutdown", "DriverMissing"));
    }
}

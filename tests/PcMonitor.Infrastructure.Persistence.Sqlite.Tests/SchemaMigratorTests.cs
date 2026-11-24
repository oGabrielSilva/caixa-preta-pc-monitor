using Dapper;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SchemaMigratorTests
{
    private const int LatestVersion = 6;

    [Fact]
    public void Migrate_creates_the_schema_and_sets_user_version()
    {
        using var temp = new TempDatabase();

        new SchemaMigrator(temp.Database).Migrate().ShouldBe(LatestVersion);

        using var connection = temp.Database.OpenWriter();
        connection.ExecuteScalar<long>("PRAGMA user_version").ShouldBe(LatestVersion);
        connection.Query<string>("SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name")
            .ShouldBe(["aggregate_1m", "diagnosis", "frame", "incident", "incident_event", "segment", "segment_sensor", "sensor", "session"]);
    }

    [Fact]
    public void Migrate_is_idempotent()
    {
        using var temp = new TempDatabase();
        var migrator = new SchemaMigrator(temp.Database);

        migrator.Migrate();

        migrator.Migrate().ShouldBe(LatestVersion);
    }

    [Fact]
    public void Version_5_recovers_the_sleep_state_from_the_event_data()
    {
        using var temp = new TempDatabase();
        var migrator = new SchemaMigrator(temp.Database);
        migrator.Migrate(upTo: 4);
        using (var connection = temp.Database.OpenWriter())
        {
            connection.Execute("""
                INSERT INTO incident (event_record_id, logged_at_ms, failure_time_source, bugcheck_code, sleep_in_progress,
                                      long_power_button, data_json)
                VALUES (1, 0, 'Unknown', 0, 1, 0, '{"SleepInProgress":"4"}'),
                       (2, 1, 'Unknown', 0, 0, 0, '{"SleepInProgress":"0"}');
                """);
        }

        migrator.Migrate();

        using var check = temp.Database.OpenReader();
        check.Query<long>("SELECT sleep_in_progress FROM incident ORDER BY event_record_id").ShouldBe([4L, 0L]);
    }

    [Fact]
    public void Migrate_stops_at_the_requested_version()
    {
        using var temp = new TempDatabase();

        new SchemaMigrator(temp.Database).Migrate(upTo: 1).ShouldBe(1);
    }

    [Fact]
    public void Version_2_moves_sensor_labels_into_each_segment_keeping_recorded_data()
    {
        using var temp = new TempDatabase();
        var migrator = new SchemaMigrator(temp.Database);
        migrator.Migrate(upTo: 1);
        using (var connection = temp.Database.OpenWriter())
        {
            connection.Execute("""
                INSERT INTO session (id, started_at_ms, booted_at_ms, source_status) VALUES (1, 0, 0, 'Ok');
                INSERT INTO segment (id, session_id, started_at_ms) VALUES (1, 1, 0);
                INSERT INTO sensor (id, stable_key, name, kind, unit, role) VALUES (7, '/v4', '+12V', 'Voltage', 'V', 'Rail12V');
                INSERT INTO segment_sensor (segment_id, ordinal, sensor_id) VALUES (1, 0, 7);
                """);
        }

        migrator.Migrate().ShouldBe(LatestVersion);

        new Frames.SqliteFrameReader(temp.Database).CatalogOf(1).Sensors.ShouldBe(
            [new PcMonitor.Domain.Sensors.SensorDescriptor("/v4", "+12V", PcMonitor.Domain.Sensors.SensorKind.Voltage, "V",
                PcMonitor.Domain.Sensors.SensorRole.Rail12V)]);
        using var check = temp.Database.OpenReader();
        check.Query<string>("SELECT name FROM pragma_table_info('sensor') ORDER BY cid").ShouldBe(["id", "stable_key"]);
    }
}

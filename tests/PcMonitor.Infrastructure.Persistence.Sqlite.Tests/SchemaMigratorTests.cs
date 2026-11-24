using Dapper;
using PcMonitor.Infrastructure.Persistence.Sqlite.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SchemaMigratorTests
{
    [Fact]
    public void Migrate_creates_the_schema_and_sets_user_version()
    {
        using var temp = new TempDatabase();

        new SchemaMigrator(temp.Database).Migrate().ShouldBe(1);

        using var connection = temp.Database.OpenWriter();
        connection.ExecuteScalar<long>("PRAGMA user_version").ShouldBe(1);
        connection.Query<string>("SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name")
            .ShouldBe(["aggregate_1m", "frame", "segment", "segment_sensor", "sensor", "session"]);
    }

    [Fact]
    public void Migrate_is_idempotent()
    {
        using var temp = new TempDatabase();
        var migrator = new SchemaMigrator(temp.Database);

        migrator.Migrate();

        migrator.Migrate().ShouldBe(1);
    }
}

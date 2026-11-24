using Dapper;
using PcMonitor.Application.Storage;
using PcMonitor.Infrastructure.Persistence.Sqlite.Storage;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SqliteStorageStatusTests
{
    [Fact]
    public void Missing_file_is_missing()
    {
        using var temp = new TempDatabase();

        new SqliteStorageStatus(temp.Database).Check().ShouldBe(StorageState.Missing);
        File.Exists(temp.Path).ShouldBeFalse();
    }

    [Fact]
    public void Current_schema_is_ready()
    {
        using var temp = TempDatabase.Migrated();

        new SqliteStorageStatus(temp.Database).Check().ShouldBe(StorageState.Ready);
    }

    [Fact]
    public void Older_schema_is_too_old()
    {
        using var temp = new TempDatabase();
        new SchemaMigrator(temp.Database).Migrate(upTo: SchemaMigrator.LatestVersion - 1);

        new SqliteStorageStatus(temp.Database).Check().ShouldBe(StorageState.SchemaTooOld);
    }

    [Fact]
    public void Newer_schema_is_too_new()
    {
        using var temp = TempDatabase.Migrated();
        using (var connection = temp.Database.OpenWriter())
        {
            connection.Execute($"PRAGMA user_version = {SchemaMigrator.LatestVersion + 1}");
        }

        new SqliteStorageStatus(temp.Database).Check().ShouldBe(StorageState.SchemaTooNew);
    }

    [Fact]
    public void Default_path_is_under_program_data() =>
        SqliteDatabase.DefaultPath.ShouldBe(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PcMonitor", "pcmonitor.db"));
}

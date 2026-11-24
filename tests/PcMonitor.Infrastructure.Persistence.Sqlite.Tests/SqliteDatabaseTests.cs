using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SqliteDatabaseTests
{
    [Fact]
    public void Writer_uses_wal_and_full_synchronous()
    {
        using var temp = new TempDatabase();
        using var connection = temp.Database.OpenWriter();

        connection.ExecuteScalar<string>("PRAGMA journal_mode").ShouldBe("wal");
        connection.ExecuteScalar<long>("PRAGMA synchronous").ShouldBe(2);
        connection.ExecuteScalar<long>("PRAGMA foreign_keys").ShouldBe(1);
    }

    [Fact]
    public void Writer_creates_the_database_folder()
    {
        using var temp = new TempDatabase();
        var nested = new SqliteDatabase(Path.Combine(temp.Folder, "a", "b", "pcmonitor.db"));

        using var connection = nested.OpenWriter();

        File.Exists(Path.Combine(temp.Folder, "a", "b", "pcmonitor.db")).ShouldBeTrue();
    }

    [Fact]
    public void Wal_files_survive_closing_every_connection()
    {
        using var temp = new TempDatabase();
        using (var connection = temp.Database.OpenWriter())
        {
            connection.Execute("CREATE TABLE t (x INTEGER); INSERT INTO t VALUES (1);");
        }

        SqliteConnection.ClearAllPools();

        File.Exists(temp.Path + "-wal").ShouldBeTrue();
        File.Exists(temp.Path + "-shm").ShouldBeTrue();
    }
}

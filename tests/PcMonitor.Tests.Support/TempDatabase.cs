using Microsoft.Data.Sqlite;
using PcMonitor.Infrastructure.Persistence.Sqlite;

namespace PcMonitor.Tests.Support;

/// <summary>A database file in its own temporary folder, deleted (with its WAL files) on dispose.</summary>
public sealed class TempDatabase : IDisposable
{
    public TempDatabase()
    {
        Folder = Directory.CreateTempSubdirectory("pcmonitor-db-").FullName;
        Path = System.IO.Path.Combine(Folder, "pcmonitor.db");
        Database = new SqliteDatabase(Path);
    }

    public string Folder { get; }

    public string Path { get; }

    public SqliteDatabase Database { get; }

    /// <summary>Opens a writer, migrates the schema and returns the database ready for use.</summary>
    public static TempDatabase Migrated()
    {
        var temp = new TempDatabase();
        new SchemaMigrator(temp.Database).Migrate();
        return temp;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(Folder, recursive: true);
    }
}

using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Infrastructure.Persistence.Sqlite.Native;

namespace PcMonitor.Infrastructure.Persistence.Sqlite;

/// <summary>
/// The PC Monitor database file. Writers get the durability settings of ADR 0004; readers are read-only.
/// </summary>
public sealed class SqliteDatabase(string path)
{
    private const string WriterPragmas = """
        PRAGMA journal_mode = WAL;
        PRAGMA synchronous = FULL;
        PRAGMA foreign_keys = ON;
        PRAGMA busy_timeout = 5000;
        """;

    public string Path { get; } = System.IO.Path.GetFullPath(path);

    /// <summary>Opens a read-write connection: WAL, <c>synchronous=FULL</c>, foreign keys and persistent WAL.</summary>
    public SqliteConnection OpenWriter()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var connection = Open(SqliteOpenMode.ReadWriteCreate);
        connection.Execute(WriterPragmas);
        PersistentWal.Enable(connection);
        return connection;
    }

    /// <summary>Opens a read-only connection. It never writes to the database file.</summary>
    public SqliteConnection OpenReader()
    {
        var connection = Open(SqliteOpenMode.ReadOnly);
        connection.Execute("PRAGMA busy_timeout = 5000;");
        return connection;
    }

    private SqliteConnection Open(SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path,
            Mode = mode,
            Pooling = false,
        }.ToString());
        connection.Open();
        return connection;
    }
}

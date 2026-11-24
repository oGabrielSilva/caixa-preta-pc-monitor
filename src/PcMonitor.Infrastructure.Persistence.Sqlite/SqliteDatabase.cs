using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Infrastructure.Persistence.Sqlite.Native;

namespace PcMonitor.Infrastructure.Persistence.Sqlite;

/// <summary>
/// The PC Monitor database file. Writers get the durability settings of ADR 0004; readers are read-only.
/// </summary>
public sealed class SqliteDatabase(string path)
{
    /// <summary>
    /// How long a writer waits for a lock held by another connection. Short on purpose: a busy database costs the
    /// sampler one frame and it retries next second, well inside the 10 s stall timeout. Both SQLite's
    /// <c>busy_timeout</c> and Microsoft.Data.Sqlite's command timeout (30 s by default) are set to it.
    /// </summary>
    private const int WriterLockWaitSeconds = 2;

    private static readonly string _writerPragmas = $"""
        PRAGMA journal_mode = WAL;
        PRAGMA synchronous = FULL;
        PRAGMA foreign_keys = ON;
        PRAGMA busy_timeout = {WriterLockWaitSeconds * 1000};
        """;

    public string Path { get; } = System.IO.Path.GetFullPath(path);

    /// <summary>Opens a read-write connection: WAL, <c>synchronous=FULL</c>, foreign keys and persistent WAL.</summary>
    public SqliteConnection OpenWriter()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var connection = Open(SqliteOpenMode.ReadWriteCreate, WriterLockWaitSeconds);
        connection.Execute(_writerPragmas);
        PersistentWal.Enable(connection);
        return connection;
    }

    /// <summary>Opens a read-only connection. It never writes to the database file.</summary>
    public SqliteConnection OpenReader()
    {
        var connection = Open(SqliteOpenMode.ReadOnly, defaultTimeoutSeconds: 30);
        connection.Execute("PRAGMA busy_timeout = 5000;");
        return connection;
    }

    private SqliteConnection Open(SqliteOpenMode mode, int defaultTimeoutSeconds)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path,
            Mode = mode,
            Pooling = false,
            DefaultTimeout = defaultTimeoutSeconds,
        }.ToString());
        connection.Open();
        return connection;
    }
}

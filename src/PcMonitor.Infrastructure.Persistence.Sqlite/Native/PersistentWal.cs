using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Native;

/// <summary>
/// Keeps the <c>-wal</c> and <c>-shm</c> files when the last connection closes, so a reader without write access to
/// the folder can still open the database (ADR 0004). SQLitePCLRaw does not expose <c>sqlite3_file_control</c>,
/// so it is called directly on the bundled <c>e_sqlite3</c> library.
/// </summary>
internal static partial class PersistentWal
{
    private const int SqliteFcntlPersistWal = 10;

    public static void Enable(SqliteConnection connection)
    {
        var handle = connection.Handle ?? throw new InvalidOperationException("The connection is not open.");
        var enabled = 1;
        var result = sqlite3_file_control(handle.DangerousGetHandle(), "main", SqliteFcntlPersistWal, ref enabled);
        if (result != 0)
        {
            throw new InvalidOperationException($"Enabling persistent WAL failed with SQLite error {result}.");
        }
    }

    [LibraryImport("e_sqlite3", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int sqlite3_file_control(IntPtr db, string dbName, int op, ref int arg);
}

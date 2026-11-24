using Microsoft.Data.Sqlite;
using PcMonitor.Application.Storage;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Storage;

/// <summary>Compares the database's schema version with the one this build reads. Never creates the file.</summary>
public sealed class SqliteStorageStatus(SqliteDatabase database) : IStorageStatus
{
    public StorageState Check()
    {
        int? version;
        try
        {
            version = database.SchemaVersion();
        }
        catch (SqliteException)
        {
            return StorageState.Unreadable;
        }
        catch (UnauthorizedAccessException)
        {
            return StorageState.Unreadable;
        }

        return version switch
        {
            null => StorageState.Missing,
            < 0 => StorageState.Unreadable,
            var v when v < SchemaMigrator.LatestVersion => StorageState.SchemaTooOld,
            var v when v > SchemaMigrator.LatestVersion => StorageState.SchemaTooNew,
            _ => StorageState.Ready,
        };
    }
}

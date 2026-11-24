using PcMonitor.Application.Storage;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Storage;

/// <summary>Compares the database's schema version with the one this build reads. Never creates the file.</summary>
public sealed class SqliteStorageStatus(SqliteDatabase database) : IStorageStatus
{
    public StorageState Check() => database.SchemaVersion() switch
    {
        null => StorageState.Missing,
        var version when version < SchemaMigrator.LatestVersion => StorageState.SchemaTooOld,
        var version when version > SchemaMigrator.LatestVersion => StorageState.SchemaTooNew,
        _ => StorageState.Ready,
    };
}

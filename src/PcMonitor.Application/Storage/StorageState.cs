namespace PcMonitor.Application.Storage;

/// <summary>Whether the database can be read by this build.</summary>
public enum StorageState
{
    /// <summary>The database exists and its schema is the one this build reads.</summary>
    Ready,

    /// <summary>No database yet: the collector was never installed or has not started.</summary>
    Missing,

    /// <summary>The database is older than this build: the collector needs updating.</summary>
    SchemaTooOld,

    /// <summary>The database is newer than this build: the dashboard needs updating.</summary>
    SchemaTooNew,
}

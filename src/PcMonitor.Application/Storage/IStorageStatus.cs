namespace PcMonitor.Application.Storage;

/// <summary>Checks the database before it is read. Never creates or migrates it.</summary>
public interface IStorageStatus
{
    StorageState Check();
}

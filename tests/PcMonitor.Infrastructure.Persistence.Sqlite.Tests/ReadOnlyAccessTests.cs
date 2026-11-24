using System.Runtime.Versioning;
using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

[SupportedOSPlatform("windows")]
public sealed class ReadOnlyAccessTests
{
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly SensorCatalog _catalog = new([new SensorDescriptor("/a", "a", SensorKind.Voltage, "V", SensorRole.None)]);

    private static void WriteFrames(SqliteMonitoringStore store, int count)
    {
        var session = store.StartSession(_t0, _t0, new SensorSourceInfo(SensorSourceStatus.Ok, null, _catalog));
        var segment = store.StartSegment(session, _t0, _catalog);
        for (var i = 0; i < count; i++)
        {
            store.AppendFrame(segment, new Frame(_t0.AddSeconds(i), [i]));
        }
    }

    [Fact]
    public void Reader_opens_while_the_writer_is_closed_and_the_folder_is_not_writable()
    {
        using var temp = TempDatabase.Migrated();
        using (var store = new SqliteMonitoringStore(temp.Database))
        {
            WriteFrames(store, 3);
        }

        SqliteConnection.ClearAllPools();

        using (new WriteDenied(temp.Folder))
        {
            using var reader = temp.Database.OpenReader();
            reader.ExecuteScalar<long>("SELECT COUNT(*) FROM frame").ShouldBe(3);
        }
    }

    [Fact]
    public void Reader_sees_frames_committed_by_an_open_writer()
    {
        using var temp = TempDatabase.Migrated();
        using var store = new SqliteMonitoringStore(temp.Database);
        WriteFrames(store, 2);

        using var reader = temp.Database.OpenReader();
        reader.ExecuteScalar<long>("SELECT COUNT(*) FROM frame").ShouldBe(2);
    }
}

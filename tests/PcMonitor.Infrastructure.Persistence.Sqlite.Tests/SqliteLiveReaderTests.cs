using System.Runtime.Versioning;
using Microsoft.Data.Sqlite;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Infrastructure.Persistence.Sqlite.Live;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

[SupportedOSPlatform("windows")]
public sealed class SqliteLiveReaderTests : IDisposable
{
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 17, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _temp = TempDatabase.Migrated();
    private readonly DatabaseSeed _seed;
    private readonly SqliteLiveReader _reader;

    public SqliteLiveReaderTests()
    {
        _seed = new DatabaseSeed(_temp.Database);
        _reader = new SqliteLiveReader(_temp.Database);
    }

    public void Dispose()
    {
        _seed.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void Latest_returns_the_last_segment_within_the_window()
    {
        var (session, segment) = _seed.Recording(_t0, 900);

        var snapshot = _reader.Latest(TimeSpan.FromMinutes(10)).ShouldNotBeNull();

        (snapshot.Session.Id, snapshot.SegmentId).ShouldBe((session, segment));
        snapshot.Session.BoardProduct.ShouldBe("TUF GAMING A620M-PLUS WIFI");
        snapshot.Catalog.Sensors.Select(s => s.StableKey).ShouldBe(DatabaseSeed.Catalog.Sensors.Select(s => s.StableKey));
        snapshot.Frames.Count.ShouldBe(601);
        snapshot.Frames[^1].Timestamp.ShouldBe(_t0.AddSeconds(899));
        snapshot.Frames[^1].Values[3].ShouldBe(12.19f);
    }

    [Fact]
    public void Latest_is_null_without_sessions() => _reader.Latest(TimeSpan.FromMinutes(10)).ShouldBeNull();

    [Fact]
    public void Latest_of_a_segment_without_frames_has_no_frames()
    {
        var session = _seed.StartSession(_t0);
        _seed.StartSegment(session, _t0);

        _reader.Latest(TimeSpan.FromMinutes(10)).ShouldNotBeNull().Frames.ShouldBeEmpty();
    }

    [Fact]
    public void Frames_after_returns_only_newer_frames_of_that_segment()
    {
        var (_, segment) = _seed.Recording(_t0, 10);

        _reader.FramesAfter(segment, _t0.AddSeconds(7)).Select(f => f.Timestamp).ShouldBe([_t0.AddSeconds(8), _t0.AddSeconds(9)]);
    }

    [Fact]
    public void Head_follows_a_new_segment_and_reports_the_last_frame()
    {
        var (session, _) = _seed.Recording(_t0, 5);
        var second = _seed.StartSegment(session, _t0.AddMinutes(1));
        _seed.Frames(second, _t0.AddMinutes(1), 3);

        var head = _reader.Head();

        head.SegmentId.ShouldBe(second);
        head.LastFrameAt.ShouldBe(_t0.AddMinutes(1).AddSeconds(2));
        head.Session.ShouldNotBeNull().EndedAt.ShouldBeNull();
    }

    [Fact]
    public void Head_reports_an_ended_session()
    {
        var (session, _) = _seed.Recording(_t0, 5);
        _seed.EndSession(session, _t0.AddSeconds(6), EndReason.SystemShutdown);

        var ended = _reader.Head().Session.ShouldNotBeNull();

        (ended.EndedAt, ended.EndReason).ShouldBe((_t0.AddSeconds(6), EndReason.SystemShutdown));
    }

    [Fact]
    public void Head_without_sessions_is_empty() => _reader.Head().ShouldBe(new Application.Live.LiveHead(null, null, null));

    [Fact]
    public void Reads_work_with_the_folder_write_denied()
    {
        _seed.Recording(_t0, 3);
        _seed.Dispose();
        SqliteConnection.ClearAllPools();

        using (new WriteDenied(_temp.Folder))
        {
            _reader.Latest(TimeSpan.FromMinutes(10)).ShouldNotBeNull().Frames.Count.ShouldBe(3);
        }
    }
}

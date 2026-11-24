using Dapper;
using PcMonitor.Application.Incidents;
using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;
using PcMonitor.Infrastructure.Persistence.Sqlite.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SqliteIncidentStoreTests : IDisposable
{
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);
    private static readonly SensorCatalog _catalog = new([new SensorDescriptor("/a", "a", SensorKind.Voltage, "V", SensorRole.None)]);

    private readonly TempDatabase _temp = TempDatabase.Migrated();
    private readonly SqliteMonitoringStore _monitoring;
    private readonly SqliteIncidentStore _store;

    public SqliteIncidentStoreTests()
    {
        _monitoring = new SqliteMonitoringStore(_temp.Database);
        _store = new SqliteIncidentStore(_temp.Database);
    }

    public void Dispose()
    {
        _monitoring.Dispose();
        _temp.Dispose();
    }

    internal static UnexpectedShutdown Crash(long record, DateTimeOffset loggedAt, long bugcheck = 0x9C) =>
        new(record, loggedAt, bugcheck, SleepInProgress: true, PowerButtonPressedAt: loggedAt.AddMinutes(-1),
            LongPowerButtonPress: false, LastAliveAt: loggedAt.AddMinutes(-4),
            new Dictionary<string, string> { ["BugcheckCode"] = "156", ["SleepInProgress"] = "1" });

    internal static Incident Unlinked(long record, DateTimeOffset loggedAt) =>
        new(Crash(record, loggedAt), null, FailureTimeSource.Unknown, null, null, null, []);

    private static SystemEvent Whea(long record, DateTimeOffset at) =>
        new("Microsoft-Windows-WHEA-Logger", 18, record, at, 2, new Dictionary<string, string> { ["ErrorSource"] = "3" });

    private (long Session, long Segment) SessionWithFrames(DateTimeOffset start, int frames)
    {
        var session = _monitoring.StartSession(start, start.AddMinutes(-1), new SensorSourceInfo(SensorSourceStatus.Ok, null, _catalog));
        var segment = _monitoring.StartSegment(session, start, _catalog);
        for (var i = 0; i < frames; i++)
        {
            _monitoring.AppendFrame(segment, new Frame(start.AddSeconds(i), [i]));
        }

        return (session, segment);
    }

    /// <summary>A session whose frames ended in a Gap, followed by a newer segment that never got a frame.</summary>
    private (long Session, long WithFrames, long Empty) SessionWithGapThenEmptySegment()
    {
        var (session, withFrames) = SessionWithFrames(_t0, 3);
        _monitoring.EndSegment(withFrames, _t0.AddSeconds(3), EndReason.Gap);
        return (session, withFrames, _monitoring.StartSegment(session, _t0.AddSeconds(10), _catalog));
    }

    private (long, string) EndOf(string table, long id)
    {
        using var connection = _temp.Database.OpenReader();
        return connection.QuerySingle<(long, string)>($"SELECT ended_at_ms, end_reason FROM {table} WHERE id = @id", new { id });
    }

    [Fact]
    public void SaveIncident_stores_the_incident_its_related_events_and_closes_the_session_atomically()
    {
        var (session, segment) = SessionWithFrames(_t0, 3);
        var open = _store.OpenSessions().Single();
        var crash = Crash(402399, _t0.AddMinutes(10), bugcheck: 0xC000021A);

        _store.SaveIncident(
            new Incident(crash, _t0.AddSeconds(2), FailureTimeSource.LastFrame, session, segment, _t0.AddSeconds(2),
                [Whea(5, _t0.AddSeconds(1)), Whea(6, _t0.AddSeconds(2))]),
            new SessionClosure(open, _t0.AddSeconds(2), EndReason.UnexpectedShutdown));

        using var connection = _temp.Database.OpenReader();
        connection.QuerySingle<(long, long, string, long, long, long, long)>("""
            SELECT event_record_id, estimated_failure_ms, failure_time_source, segment_id, telemetry_end_ms, bugcheck_code,
                   sleep_in_progress FROM incident
            """).ShouldBe((402399L, _t0.AddSeconds(2).ToUnixTimeMilliseconds(), "LastFrame", segment,
            _t0.AddSeconds(2).ToUnixTimeMilliseconds(), 0xC000021AL, 1L));
        connection.Query<long>("SELECT record_id FROM incident_event ORDER BY record_id").ShouldBe([5L, 6L]);
        EndOf("session", session).ShouldBe((_t0.AddSeconds(2).ToUnixTimeMilliseconds(), "UnexpectedShutdown"));
        EndOf("segment", segment).ShouldBe((_t0.AddSeconds(2).ToUnixTimeMilliseconds(), "UnexpectedShutdown"));
    }

    [Fact]
    public void SaveIncident_writes_nothing_when_the_session_closure_fails()
    {
        var bogus = new SessionClosure(new OpenSession(999, _t0, _t0, null, null, null), _t0, EndReason.UnexpectedShutdown);
        using (var connection = _temp.Database.OpenWriter())
        {
            connection.Execute("CREATE TRIGGER fail_close BEFORE UPDATE ON session BEGIN SELECT RAISE(ABORT, 'busy'); END;");
            connection.Execute("INSERT INTO session (id, started_at_ms, booted_at_ms, source_status) VALUES (999, 0, 0, 'Ok')");
        }

        Should.Throw<Microsoft.Data.Sqlite.SqliteException>(() => _store.SaveIncident(Unlinked(1, _t0), bogus));

        _store.SavedIncidents().ShouldBeEmpty();
    }

    [Fact]
    public void SaveIncident_ignores_a_crash_already_saved()
    {
        _store.SaveIncident(Unlinked(1, _t0), null);

        _store.SaveIncident(Unlinked(1, _t0), null);

        _store.SavedIncidents().ShouldBe([new IncidentKey(1, _t0)]);
    }

    [Fact]
    public void OpenSessions_lists_sessions_without_end_reason_with_boot_segments_and_last_frame()
    {
        var (closed, _) = SessionWithFrames(_t0, 2);
        _monitoring.EndSession(closed, _t0.AddMinutes(1), EndReason.ServiceStopped);
        var (sleeping, first) = SessionWithFrames(_t0.AddHours(1), 3);
        _monitoring.EndSegment(first, _t0.AddHours(1).AddSeconds(5), EndReason.Suspended);
        var (running, open) = SessionWithFrames(_t0.AddHours(2), 4);

        _store.OpenSessions().ShouldBe(
        [
            new OpenSession(sleeping, _t0.AddHours(1), _t0.AddHours(1).AddMinutes(-1), first, null, _t0.AddHours(1).AddSeconds(2)),
            new OpenSession(running, _t0.AddHours(2), _t0.AddHours(2).AddMinutes(-1), open, open, _t0.AddHours(2).AddSeconds(3)),
        ]);
    }

    [Fact]
    public void OpenSessions_points_to_the_segment_with_the_last_frame_not_a_newer_empty_one()
    {
        var (_, withFrames, empty) = SessionWithGapThenEmptySegment();

        var open = _store.OpenSessions().ShouldHaveSingleItem();

        (open.LastSegmentId, open.OpenSegmentId, open.LastFrameAt).ShouldBe((withFrames, empty, _t0.AddSeconds(2)));
    }

    [Fact]
    public void CloseSession_never_ends_an_empty_open_segment_before_it_started()
    {
        var (session, _, empty) = SessionWithGapThenEmptySegment();
        var open = _store.OpenSessions().Single();

        _store.CloseSession(new SessionClosure(open, _t0.AddSeconds(2), EndReason.Abnormal));

        EndOf("segment", empty).ShouldBe((_t0.AddSeconds(10).ToUnixTimeMilliseconds(), "Abnormal"));
        EndOf("session", session).ShouldBe((_t0.AddSeconds(2).ToUnixTimeMilliseconds(), "Abnormal"));
        _store.OpenSessions().ShouldBeEmpty();
    }
}

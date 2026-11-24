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

    internal static UnexpectedShutdown Crash(long record, DateTimeOffset loggedAt) =>
        new(record, loggedAt, BugcheckCode: 0x9C, SleepInProgress: true, PowerButtonPressedAt: loggedAt.AddMinutes(-1),
            LongPowerButtonPress: false, LastAliveAt: loggedAt.AddMinutes(-4),
            new Dictionary<string, string> { ["BugcheckCode"] = "156", ["SleepInProgress"] = "1" });

    private static SystemEvent Whea(long record, DateTimeOffset at) =>
        new("Microsoft-Windows-WHEA-Logger", 18, record, at, 2, new Dictionary<string, string> { ["ErrorSource"] = "3" });

    private (long Session, long Segment) SessionWithFrames(DateTimeOffset start, int frames)
    {
        var session = _monitoring.StartSession(start, start, new SensorSourceInfo(SensorSourceStatus.Ok, null, _catalog));
        var segment = _monitoring.StartSegment(session, start, _catalog);
        for (var i = 0; i < frames; i++)
        {
            _monitoring.AppendFrame(segment, new Frame(start.AddSeconds(i), [i]));
        }

        return (session, segment);
    }

    [Fact]
    public void SaveIncident_stores_the_incident_and_its_related_events()
    {
        var (session, segment) = SessionWithFrames(_t0, 3);
        var crash = Crash(402399, _t0.AddMinutes(10));

        _store.SaveIncident(new Incident(crash, _t0.AddSeconds(2), FailureTimeSource.LastFrame, session, segment,
            [Whea(5, _t0.AddSeconds(1)), Whea(6, _t0.AddSeconds(2))])).ShouldBeTrue();

        using var connection = _temp.Database.OpenReader();
        connection.QuerySingle<(long, long, long, string, long, long, long, long, long)>("""
            SELECT event_record_id, logged_at_ms, estimated_failure_ms, failure_time_source, segment_id,
                   bugcheck_code, sleep_in_progress, power_button_ms, last_alive_ms FROM incident
            """).ShouldBe((402399L, crash.LoggedAt.ToUnixTimeMilliseconds(), _t0.AddSeconds(2).ToUnixTimeMilliseconds(),
            "LastFrame", segment, 0x9CL, 1L, crash.PowerButtonPressedAt!.Value.ToUnixTimeMilliseconds(),
            crash.LastAliveAt!.Value.ToUnixTimeMilliseconds()));
        connection.QuerySingle<string>("SELECT data_json FROM incident").ShouldContain("\"BugcheckCode\":\"156\"");
        connection.Query<long>("SELECT record_id FROM incident_event ORDER BY record_id").ShouldBe([5L, 6L]);
    }

    [Fact]
    public void SaveIncident_returns_false_for_a_crash_already_saved()
    {
        var incident = new Incident(Crash(1, _t0), null, FailureTimeSource.Unknown, null, null, []);
        _store.SaveIncident(incident);

        _store.SaveIncident(incident).ShouldBeFalse();

        using var connection = _temp.Database.OpenReader();
        connection.ExecuteScalar<long>("SELECT COUNT(*) FROM incident").ShouldBe(1);
    }

    [Fact]
    public void LatestIncidentLoggedAt_is_the_newest_crash_or_null()
    {
        _store.LatestIncidentLoggedAt().ShouldBeNull();
        _store.SaveIncident(new Incident(Crash(1, _t0), null, FailureTimeSource.Unknown, null, null, []));
        _store.SaveIncident(new Incident(Crash(2, _t0.AddHours(1)), null, FailureTimeSource.Unknown, null, null, []));

        _store.LatestIncidentLoggedAt().ShouldBe(_t0.AddHours(1));
    }

    [Fact]
    public void OpenSessions_lists_sessions_without_end_reason_with_their_last_and_open_segments_and_last_frame()
    {
        var (closed, _) = SessionWithFrames(_t0, 2);
        _monitoring.EndSession(closed, _t0.AddMinutes(1), EndReason.ServiceStopped);
        var (sleeping, first) = SessionWithFrames(_t0.AddHours(1), 3);
        _monitoring.EndSegment(first, _t0.AddHours(1).AddSeconds(5), EndReason.Suspended);
        var (running, open) = SessionWithFrames(_t0.AddHours(2), 4);

        _store.OpenSessions().ShouldBe(
        [
            new OpenSession(sleeping, _t0.AddHours(1), first, null, _t0.AddHours(1).AddSeconds(2)),
            new OpenSession(running, _t0.AddHours(2), open, open, _t0.AddHours(2).AddSeconds(3)),
        ]);
    }

    [Fact]
    public void EndSession_closes_the_open_segment_and_the_session_with_time_and_reason()
    {
        SessionWithFrames(_t0, 2);
        var session = _store.OpenSessions().Single();

        _store.EndSession(session, _t0.AddSeconds(1), EndReason.UnexpectedShutdown);

        _store.OpenSessions().ShouldBeEmpty();
        using var connection = _temp.Database.OpenReader();
        var at = _t0.AddSeconds(1).ToUnixTimeMilliseconds();
        connection.QuerySingle<(long, string)>("SELECT ended_at_ms, end_reason FROM segment").ShouldBe((at, "UnexpectedShutdown"));
        connection.QuerySingle<(long, string)>("SELECT ended_at_ms, end_reason FROM session").ShouldBe((at, "UnexpectedShutdown"));
    }
}

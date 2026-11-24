using Microsoft.Extensions.Logging.Abstractions;
using PcMonitor.Application.Incidents;
using PcMonitor.Application.Tests.Fakes;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Tests.Incidents;

public sealed class DetectIncidentsTests
{
    private static readonly DateTimeOffset _day = new(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeSystemEventLog _eventLog = new();
    private readonly InMemoryIncidentStore _store = new();

    private static DateTimeOffset At(int hour, int minute, int second = 0) => _day.AddHours(hour).AddMinutes(minute).AddSeconds(second);

    private static UnexpectedShutdown Crash(long record, DateTimeOffset loggedAt, DateTimeOffset? lastAlive = null, bool sleeping = false) =>
        new(record, loggedAt, BugcheckCode: 0, sleeping, PowerButtonPressedAt: null, LongPowerButtonPress: false, lastAlive,
            new Dictionary<string, string>());

    private DetectionResult Detect() => new DetectIncidents(_eventLog, _store, NullLogger<DetectIncidents>.Instance).Run();

    [Fact]
    public void Crash_with_an_open_session_uses_the_last_frame_as_failure_time()
    {
        _store.Open.Add(new OpenSession(1, At(13, 0), LastSegmentId: 4, OpenSegmentId: 4, LastFrameAt: At(13, 50, 30)));
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38), lastAlive: At(13, 50, 34)));

        Detect().ShouldBe(new DetectionResult(Incidents: 1, WithTelemetry: 1, AbnormalSessions: 0));

        var incident = _store.Incidents.ShouldHaveSingleItem();
        incident.EstimatedFailureAt.ShouldBe(At(13, 50, 30));
        incident.FailureTimeSource.ShouldBe(FailureTimeSource.LastFrame);
        (incident.SessionId, incident.SegmentId).ShouldBe((1L, 4L));
    }

    [Fact]
    public void Crash_without_a_session_uses_the_event_log_last_alive_time()
    {
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38), lastAlive: At(13, 50, 34)));

        Detect();

        var incident = _store.Incidents.ShouldHaveSingleItem();
        incident.EstimatedFailureAt.ShouldBe(At(13, 50, 34));
        incident.FailureTimeSource.ShouldBe(FailureTimeSource.LastAliveEventLog);
        incident.SessionId.ShouldBeNull();
        incident.SegmentId.ShouldBeNull();
    }

    [Fact]
    public void Crash_without_session_or_last_alive_has_an_unknown_failure_time()
    {
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38)));

        Detect();

        var incident = _store.Incidents.ShouldHaveSingleItem();
        incident.EstimatedFailureAt.ShouldBeNull();
        incident.FailureTimeSource.ShouldBe(FailureTimeSource.Unknown);
    }

    [Fact]
    public void Crashed_session_is_ended_as_UnexpectedShutdown_at_its_last_frame()
    {
        var session = new OpenSession(1, At(13, 0), 4, 4, At(13, 50, 30));
        _store.Open.Add(session);
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38)));

        Detect();

        _store.Ended.ShouldHaveSingleItem().ShouldBe((session, At(13, 50, 30), EndReason.UnexpectedShutdown));
    }

    [Fact]
    public void Two_crashes_and_one_open_session_give_telemetry_only_to_the_first_crash_after_the_session_started()
    {
        _store.Open.Add(new OpenSession(1, At(13, 0), 4, 4, At(13, 20)));
        _eventLog.Shutdowns.Add(Crash(10, At(13, 25)));
        _eventLog.Shutdowns.Add(Crash(11, At(13, 30), lastAlive: At(13, 26)));

        Detect().ShouldBe(new DetectionResult(Incidents: 2, WithTelemetry: 1, AbnormalSessions: 0));

        _store.Incidents[0].SessionId.ShouldBe(1);
        _store.Incidents[1].SessionId.ShouldBeNull();
        _store.Incidents[1].FailureTimeSource.ShouldBe(FailureTimeSource.LastAliveEventLog);
    }

    [Fact]
    public void Crash_during_sleep_uses_the_last_closed_segment_and_keeps_SleepInProgress()
    {
        _store.Open.Add(new OpenSession(1, At(13, 0), LastSegmentId: 7, OpenSegmentId: null, LastFrameAt: At(22, 0)));
        _eventLog.Shutdowns.Add(Crash(10, At(23, 30), sleeping: true));

        Detect();

        var incident = _store.Incidents.ShouldHaveSingleItem();
        incident.SegmentId.ShouldBe(7);
        incident.Shutdown.SleepInProgress.ShouldBeTrue();
    }

    [Fact]
    public void Cleanly_closed_sessions_produce_no_incident_and_are_not_touched()
    {
        Detect().ShouldBe(new DetectionResult(0, 0, 0));

        _store.Incidents.ShouldBeEmpty();
        _store.Ended.ShouldBeEmpty();
    }

    [Fact]
    public void Open_session_without_a_crash_event_is_ended_as_Abnormal()
    {
        var session = new OpenSession(1, At(13, 0), 4, 4, LastFrameAt: null);
        _store.Open.Add(session);

        Detect().AbnormalSessions.ShouldBe(1);

        _store.Ended.ShouldHaveSingleItem().ShouldBe((session, At(13, 0), EndReason.Abnormal));
        _store.Incidents.ShouldBeEmpty();
    }

    [Fact]
    public void Already_saved_crashes_are_not_saved_again()
    {
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38)));
        Detect();

        Detect().Incidents.ShouldBe(0);

        _store.Incidents.ShouldHaveSingleItem();
    }

    [Fact]
    public void Related_events_are_read_from_ten_minutes_before_the_estimate_to_two_minutes_after_the_event()
    {
        _store.Open.Add(new OpenSession(1, At(13, 0), 4, 4, At(13, 50, 30)));
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38)));
        _eventLog.Events.Add(new SystemEvent("Microsoft-Windows-WHEA-Logger", 18, 99, At(13, 50, 29), 2, new Dictionary<string, string>()));

        Detect();

        _eventLog.RelatedQueries.ShouldHaveSingleItem().ShouldBe((At(13, 40, 30), At(13, 56, 38)));
        _store.Incidents.ShouldHaveSingleItem().RelatedEvents.ShouldHaveSingleItem().EventId.ShouldBe(18);
    }

    [Fact]
    public void Without_an_estimate_related_events_start_forty_minutes_before_the_event()
    {
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38)));

        Detect();

        _eventLog.RelatedQueries.ShouldHaveSingleItem().ShouldBe((At(13, 14, 38), At(13, 56, 38)));
    }
}

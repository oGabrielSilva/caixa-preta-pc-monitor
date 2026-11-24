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
        new(record, loggedAt, BugcheckCode: 0, SleepState: sleeping ? 4 : 0, PowerButtonPressedAt: null, LongPowerButtonPress: false, lastAlive,
            new Dictionary<string, string>());

    /// <summary>A session of the boot at <paramref name="bootedAt"/>, started a minute later.</summary>
    private static OpenSession Session(long id, DateTimeOffset bootedAt, DateTimeOffset? lastFrame, long segment = 4, long? open = 4) =>
        new(id, bootedAt.AddMinutes(1), bootedAt, segment, open, lastFrame);

    private DetectionResult Detect() => new DetectIncidents(_eventLog, _store, _store, NullLogger<DetectIncidents>.Instance).Run();

    [Fact]
    public void Crash_with_an_open_session_uses_the_last_frame_as_failure_time()
    {
        _store.Open.Add(Session(1, At(13, 0), lastFrame: At(13, 50, 30)));
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38), lastAlive: At(13, 40)));

        Detect().ShouldBe(new DetectionResult(Incidents: 1, WithTelemetry: 1, AbnormalSessions: 0));

        var incident = _store.Incidents.ShouldHaveSingleItem();
        incident.EstimatedFailureAt.ShouldBe(At(13, 50, 30));
        incident.FailureTimeSource.ShouldBe(FailureTimeSource.LastFrame);
        (incident.SessionId, incident.SegmentId, incident.TelemetryEndAt).ShouldBe((1L, 4L, At(13, 50, 30)));
    }

    [Fact]
    public void Incident_and_the_closure_of_its_session_are_saved_together()
    {
        var session = Session(1, At(13, 0), lastFrame: At(13, 50, 30));
        _store.Open.Add(session);
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38)));

        Detect();

        _store.Saved.ShouldHaveSingleItem().Closure
            .ShouldBe(new SessionClosure(session, At(13, 50, 30), EndReason.UnexpectedShutdown));
        _store.Closed.ShouldBeEmpty();
    }

    [Fact]
    public void Crash_without_a_session_uses_the_event_log_last_alive_time()
    {
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38), lastAlive: At(13, 50, 34)));

        Detect();

        var (incident, closure) = _store.Saved.ShouldHaveSingleItem();
        incident.EstimatedFailureAt.ShouldBe(At(13, 50, 34));
        incident.FailureTimeSource.ShouldBe(FailureTimeSource.LastAliveEventLog);
        (incident.SessionId, incident.SegmentId, incident.TelemetryEndAt, closure).ShouldBe((null, null, null, null));
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
    public void Collector_that_stopped_before_the_crash_does_not_claim_the_failure_time()
    {
        // Last frame at 09:00, but Windows was still alive at 13:40: the collector was not recording when it failed.
        _store.Open.Add(Session(1, At(8, 0), lastFrame: At(9, 0)));
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38), lastAlive: At(13, 40)));

        Detect().WithTelemetry.ShouldBe(1);

        var incident = _store.Incidents.ShouldHaveSingleItem();
        incident.EstimatedFailureAt.ShouldBe(At(13, 40));
        incident.FailureTimeSource.ShouldBe(FailureTimeSource.LastAliveEventLog);
        incident.TelemetryEndAt.ShouldBe(At(9, 0));
    }

    [Fact]
    public void Crash_during_sleep_is_marked_as_after_the_last_frame_before_sleep()
    {
        _store.Open.Add(Session(1, At(13, 0), lastFrame: At(22, 0), segment: 7, open: null));
        _eventLog.Shutdowns.Add(Crash(10, At(23, 30), sleeping: true));

        Detect();

        var incident = _store.Incidents.ShouldHaveSingleItem();
        (incident.SegmentId, incident.EstimatedFailureAt, incident.FailureTimeSource)
            .ShouldBe((7L, At(22, 0), FailureTimeSource.LastFrameBeforeSleep));
        incident.Shutdown.SleepInProgress.ShouldBeTrue();
    }

    [Fact]
    public void Second_crash_before_the_collector_started_again_gets_no_telemetry()
    {
        _store.Open.Add(Session(1, At(13, 0), lastFrame: At(13, 20)));
        _eventLog.Shutdowns.Add(Crash(10, At(13, 25)));
        _eventLog.Shutdowns.Add(Crash(11, At(13, 30), lastAlive: At(13, 26)));

        Detect().ShouldBe(new DetectionResult(Incidents: 2, WithTelemetry: 1, AbnormalSessions: 0));

        _store.Incidents[0].SessionId.ShouldBe(1);
        _store.Incidents[1].SessionId.ShouldBeNull();
    }

    [Fact]
    public void Session_from_a_boot_before_an_earlier_crash_is_not_linked_to_a_later_crash()
    {
        // Crash 10 is already saved and its session closed; an older session left open (booted 12:00) must not be
        // taken by crash 11, which happened in a later boot (13:25).
        _store.Saved.Add((new Incident(Crash(10, At(13, 25)), null, FailureTimeSource.Unknown, null, null, null, []), null));
        _store.Open.Add(Session(1, At(12, 0), lastFrame: At(12, 30)));
        _eventLog.Shutdowns.Add(Crash(10, At(13, 25)));
        _eventLog.Shutdowns.Add(Crash(11, At(13, 30)));

        Detect().ShouldBe(new DetectionResult(Incidents: 1, WithTelemetry: 0, AbnormalSessions: 1));

        _store.Incidents[^1].SessionId.ShouldBeNull();
        _store.Closed.ShouldHaveSingleItem().Reason.ShouldBe(EndReason.Abnormal);
    }

    [Fact]
    public void Cleanly_closed_sessions_produce_no_incident_and_are_not_touched()
    {
        Detect().ShouldBe(new DetectionResult(0, 0, 0));

        _store.Saved.ShouldBeEmpty();
        _store.Closed.ShouldBeEmpty();
    }

    [Fact]
    public void Open_session_without_a_crash_event_is_ended_as_Abnormal()
    {
        var session = Session(1, At(13, 0), lastFrame: null);
        _store.Open.Add(session);

        Detect().AbnormalSessions.ShouldBe(1);

        _store.Closed.ShouldHaveSingleItem().ShouldBe(new SessionClosure(session, session.StartedAt, EndReason.Abnormal));
        _store.Saved.ShouldBeEmpty();
    }

    [Fact]
    public void Already_saved_crashes_are_skipped_before_matching_sessions()
    {
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38)));
        Detect();

        Detect().Incidents.ShouldBe(0);

        _store.Incidents.ShouldHaveSingleItem();
    }

    [Fact]
    public void Crash_logged_with_an_earlier_clock_than_saved_ones_is_still_imported()
    {
        // RTC reset: the newer event (record 11) carries an older timestamp.
        _store.Saved.Add((new Incident(Crash(10, At(13, 0)), null, FailureTimeSource.Unknown, null, null, null, []), null));
        _eventLog.Shutdowns.Add(Crash(10, At(13, 0)));
        _eventLog.Shutdowns.Add(Crash(11, _day.AddYears(-6)));

        Detect().Incidents.ShouldBe(1);

        _store.Incidents[^1].Shutdown.RecordId.ShouldBe(11);
    }

    [Fact]
    public void Session_without_frames_is_linked_but_not_counted_as_telemetry()
    {
        _store.Open.Add(Session(1, At(13, 0), lastFrame: null));
        _eventLog.Shutdowns.Add(Crash(10, At(13, 54, 38), lastAlive: At(13, 50)));

        Detect().ShouldBe(new DetectionResult(Incidents: 1, WithTelemetry: 0, AbnormalSessions: 0));

        var incident = _store.Incidents.ShouldHaveSingleItem();
        (incident.SessionId, incident.TelemetryEndAt, incident.FailureTimeSource).ShouldBe((1L, null, FailureTimeSource.LastAliveEventLog));
    }

    [Fact]
    public void Related_events_are_read_from_ten_minutes_before_the_estimate_to_two_minutes_after_the_event()
    {
        _store.Open.Add(Session(1, At(13, 0), lastFrame: At(13, 50, 30)));
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

    [Fact]
    public void Related_event_windows_start_after_the_previous_crash_boot_settled()
    {
        _eventLog.Shutdowns.Add(Crash(10, At(13, 0)));
        _eventLog.Shutdowns.Add(Crash(11, At(13, 5)));
        _eventLog.Shutdowns.Add(Crash(12, At(13, 6)));

        Detect();

        _eventLog.RelatedQueries[1].ShouldBe((At(13, 2) + TimeSpan.FromTicks(1), At(13, 7)));
        _eventLog.RelatedQueries[2].Since.ShouldBe(At(13, 7) + TimeSpan.FromTicks(1));
    }

    [Fact]
    public void A_crash_that_fails_to_save_does_not_stop_the_others()
    {
        _store.FailOnRecordId = 10;
        _eventLog.Shutdowns.Add(Crash(10, At(13, 0)));
        _eventLog.Shutdowns.Add(Crash(11, At(14, 0)));

        Detect().Incidents.ShouldBe(1);

        _store.Incidents.ShouldHaveSingleItem().Shutdown.RecordId.ShouldBe(11);
    }
}

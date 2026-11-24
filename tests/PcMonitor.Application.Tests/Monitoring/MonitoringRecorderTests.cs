using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PcMonitor.Application.Monitoring;
using PcMonitor.Application.Tests.Fakes;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Tests.Monitoring;

public sealed class MonitoringRecorderTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan _gapThreshold = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan _uptime = TimeSpan.FromMinutes(10);

    private readonly FakeTimeProvider _time = new(_start);
    private readonly InMemoryMonitoringStore _store = new();

    private MonitoringRecorder Recorder(ScriptedSensorSource source) =>
        new(source, _store, _time, NullLogger<MonitoringRecorder>.Instance, _gapThreshold, () => _uptime);

    [Fact]
    public void Start_creates_a_session_and_a_segment_with_the_source_catalog()
    {
        var info = Recorder(new ScriptedSensorSource()).Start();

        info.Status.ShouldBe(SensorSourceStatus.Ok);
        _store.Sessions.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            s => s.StartedAt.ShouldBe(_start),
            s => s.BootedAt.ShouldBe(_start - _uptime),
            s => s.Source.BoardProduct.ShouldBe("TEST BOARD"));
        _store.Segments.ShouldHaveSingleItem().Catalog.ShouldBeSameAs(ScriptedSensorSource.Catalog);
    }

    [Fact]
    public void RecordOnce_appends_the_values_read_with_the_current_time()
    {
        var recorder = Recorder(new ScriptedSensorSource([45f, 12.1f]));
        recorder.Start();
        _time.Advance(TimeSpan.FromSeconds(1));

        recorder.RecordOnce().ShouldBeTrue();

        var (segmentId, frame) = _store.Frames.ShouldHaveSingleItem();
        segmentId.ShouldBe(1);
        frame.Timestamp.ShouldBe(_start.AddSeconds(1));
        frame.Values.ShouldBe([45f, 12.1f]);
    }

    [Fact]
    public void RecordOnce_returns_false_and_keeps_running_when_the_store_throws()
    {
        var recorder = Recorder(new ScriptedSensorSource([1f, 1f], [2f, 2f]));
        recorder.Start();
        _store.AppendFailure = new IOException("disk full");

        recorder.RecordOnce().ShouldBeFalse();

        _store.AppendFailure = null;
        recorder.RecordOnce().ShouldBeTrue();
        _store.Frames.ShouldHaveSingleItem().Frame.Values.ShouldBe([2f, 2f]);
    }

    [Fact]
    public void RecordOnce_returns_false_when_the_sensor_read_throws()
    {
        var source = new ScriptedSensorSource { ReadFailure = new InvalidOperationException("driver") };
        var recorder = Recorder(source);
        recorder.Start();

        recorder.RecordOnce().ShouldBeFalse();
        _store.Frames.ShouldBeEmpty();
    }

    [Fact]
    public void RecordOnce_after_BreakSegment_opens_a_new_segment()
    {
        var recorder = Recorder(new ScriptedSensorSource([1f, 1f], [2f, 2f]));
        recorder.Start();
        recorder.RecordOnce();

        recorder.BreakSegment(EndReason.Suspended);
        _time.Advance(TimeSpan.FromHours(8));
        recorder.RecordOnce();

        _store.EndedSegments.ShouldHaveSingleItem().ShouldBe((1L, _start, EndReason.Suspended));
        _store.Segments.Count.ShouldBe(2);
        _store.Frames.Select(f => f.SegmentId).ShouldBe([1L, 2L]);
    }

    [Fact]
    public void RecordOnce_after_a_gap_closes_the_segment_as_Gap_at_the_last_frame_time()
    {
        var recorder = Recorder(new ScriptedSensorSource([1f, 1f], [2f, 2f]));
        recorder.Start();
        recorder.RecordOnce();

        _time.Advance(TimeSpan.FromSeconds(5));
        recorder.RecordOnce();

        _store.EndedSegments.ShouldHaveSingleItem().ShouldBe((1L, _start, EndReason.Gap));
        _store.Frames.Last().SegmentId.ShouldBe(2);
    }

    [Fact]
    public void RecordOnce_within_the_gap_threshold_keeps_the_segment()
    {
        var recorder = Recorder(new ScriptedSensorSource([1f, 1f], [2f, 2f]));
        recorder.Start();
        recorder.RecordOnce();

        _time.Advance(TimeSpan.FromSeconds(2));
        recorder.RecordOnce();

        _store.EndedSegments.ShouldBeEmpty();
        _store.Segments.ShouldHaveSingleItem();
    }

    [Fact]
    public void Stop_ends_segment_and_session_with_the_reason()
    {
        var recorder = Recorder(new ScriptedSensorSource());
        recorder.Start();
        _time.Advance(TimeSpan.FromSeconds(30));

        recorder.Stop(EndReason.SystemShutdown);

        _store.EndedSegments.ShouldHaveSingleItem().ShouldBe((1L, _start.AddSeconds(30), EndReason.SystemShutdown));
        _store.EndedSessions.ShouldHaveSingleItem().ShouldBe((1L, _start.AddSeconds(30), EndReason.SystemShutdown));
    }

    [Fact]
    public void Stop_after_BreakSegment_does_not_end_the_segment_twice()
    {
        var recorder = Recorder(new ScriptedSensorSource());
        recorder.Start();
        recorder.BreakSegment(EndReason.Suspended);

        recorder.Stop(EndReason.ServiceStopped);

        _store.EndedSegments.ShouldHaveSingleItem().Reason.ShouldBe(EndReason.Suspended);
        _store.EndedSessions.ShouldHaveSingleItem().Reason.ShouldBe(EndReason.ServiceStopped);
    }
}

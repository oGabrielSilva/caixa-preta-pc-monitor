using Microsoft.Extensions.Time.Testing;
using PcMonitor.Application.Live;
using PcMonitor.Application.Tests.Fakes;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Tests.Live;

public sealed class LiveCursorTests
{
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(_t0);
    private readonly FakeLiveReader _reader = new() { Session = LiveStateRuleTests.Session(), SegmentId = 7 };

    private static Frame At(int second) => new(_t0.AddSeconds(second), [second]);

    private LiveCursor Cursor(long? segmentId = 7, DateTimeOffset? after = null) =>
        new(_reader, _time, segmentId, after ?? _t0);

    [Fact]
    public void New_frames_are_appended_once()
    {
        var cursor = Cursor();
        cursor.Poll().ShouldBeEmpty();

        _reader.Add(7, At(1));
        var update = cursor.Poll().ShouldHaveSingleItem().ShouldBeOfType<FramesAppended>();

        update.SegmentId.ShouldBe(7);
        update.Frames.Select(f => f.Timestamp).ShouldBe([At(1).Timestamp]);
        cursor.Poll().ShouldBeEmpty();
    }

    [Fact]
    public void A_new_segment_is_announced_and_followed_from_its_last_frame()
    {
        var cursor = Cursor();
        _reader.SegmentId = 8;
        _reader.Add(8, At(1));

        cursor.Poll().ShouldHaveSingleItem().ShouldBe(new SegmentChanged(8));

        _reader.Add(8, At(2));
        cursor.Poll().ShouldHaveSingleItem().ShouldBeOfType<FramesAppended>().Frames.Select(f => f.Timestamp).ShouldBe([At(2).Timestamp]);
    }

    [Fact]
    public void A_client_without_a_segment_is_told_about_the_current_one()
    {
        var cursor = Cursor(segmentId: null);

        cursor.Poll().ShouldHaveSingleItem().ShouldBe(new SegmentChanged(7));
    }

    [Fact]
    public void State_is_reported_every_fifth_poll()
    {
        var cursor = Cursor();
        _reader.Add(7, At(0));

        for (var i = 0; i < 4; i++)
        {
            cursor.Poll().OfType<StateReported>().ShouldBeEmpty();
        }

        cursor.Poll().OfType<StateReported>().ShouldHaveSingleItem().State.ShouldBe(LiveState.Recording);
    }

    [Fact]
    public void State_uses_the_last_frame_time()
    {
        var cursor = Cursor();
        _reader.Add(7, At(0));
        _time.Advance(TimeSpan.FromSeconds(30));

        var updates = Enumerable.Range(0, 5).SelectMany(_ => cursor.Poll()).OfType<StateReported>();

        updates.ShouldHaveSingleItem().State.ShouldBe(LiveState.Stale);
    }

    [Fact]
    public void A_failed_read_is_reported_and_the_next_poll_recovers()
    {
        var cursor = Cursor();
        _reader.Add(7, At(1));
        _reader.FailuresLeft = 1;

        Should.Throw<InvalidOperationException>(cursor.Poll);
        cursor.Poll().ShouldHaveSingleItem().ShouldBeOfType<FramesAppended>();
    }
}

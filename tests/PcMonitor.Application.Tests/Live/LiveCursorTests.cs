using PcMonitor.Application.Live;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Tests.Live;

public sealed class LiveCursorTests : LiveTestClock
{
    private static Frame At(int second) => new(T0.AddSeconds(second), [second]);

    private LiveCursor Cursor(long? segmentId = 7, DateTimeOffset? after = null) =>
        new(Reader, Time, segmentId, after ?? T0);

    [Fact]
    public void New_frames_are_appended_once()
    {
        var cursor = Cursor();
        cursor.Poll().ShouldBeEmpty();

        Reader.Add(7, At(1));
        var update = cursor.Poll().ShouldHaveSingleItem().ShouldBeOfType<FramesAppended>();

        update.SegmentId.ShouldBe(7);
        update.Frames.Select(f => f.Timestamp).ShouldBe([At(1).Timestamp]);
        cursor.Poll().ShouldBeEmpty();
    }

    [Fact]
    public void A_new_segment_is_announced_and_followed_from_its_last_frame()
    {
        var cursor = Cursor();
        Reader.SegmentId = 8;
        Reader.Add(8, At(1));

        cursor.Poll().ShouldHaveSingleItem().ShouldBe(new SegmentChanged(8));

        Reader.Add(8, At(2));
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
        Reader.Add(7, At(0));

        for (var i = 0; i < 4; i++)
        {
            cursor.Poll().OfType<StateReported>().ShouldBeEmpty();
        }

        cursor.Poll().OfType<StateReported>().ShouldHaveSingleItem().State.ShouldBe(LiveState.Recording);
    }

    [Fact]
    public void State_uses_the_last_frameTime()
    {
        var cursor = Cursor();
        Reader.Add(7, At(0));
        Time.Advance(TimeSpan.FromSeconds(30));

        var updates = Enumerable.Range(0, 5).SelectMany(_ => cursor.Poll()).OfType<StateReported>();

        updates.ShouldHaveSingleItem().State.ShouldBe(LiveState.Stale);
    }

    [Fact]
    public void A_stale_position_catches_up_at_most_the_live_window()
    {
        // A tab reconnecting hours later must not receive hours of frames in one event.
        var cursor = Cursor(after: T0);
        Reader.Add(7, [.. Enumerable.Range(1, 3600).Select(At)]);

        var frames = cursor.Poll().ShouldHaveSingleItem().ShouldBeOfType<FramesAppended>().Frames;

        frames[0].Timestamp.ShouldBe(At(3600).Timestamp - LiveCursor.MaxCatchUp + TimeSpan.FromSeconds(1));
        frames[^1].Timestamp.ShouldBe(At(3600).Timestamp);
    }

    [Fact]
    public void A_failed_read_is_reported_and_the_next_poll_recovers()
    {
        var cursor = Cursor();
        Reader.Add(7, At(1));
        Reader.FailuresLeft = 1;

        Should.Throw<InvalidOperationException>(cursor.Poll);
        cursor.Poll().ShouldHaveSingleItem().ShouldBeOfType<FramesAppended>();
    }
}

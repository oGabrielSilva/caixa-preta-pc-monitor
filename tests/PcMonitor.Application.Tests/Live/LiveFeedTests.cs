using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PcMonitor.Application.Live;
using PcMonitor.Application.Tests.Fakes;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Tests.Live;

public sealed class LiveFeedTests
{
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(_t0);
    private readonly FakeLiveReader _reader = new() { Session = LiveStateRuleTests.Session(), SegmentId = 7 };

    private LiveFeed Feed() => new(_reader, _time, NullLogger<LiveFeed>.Instance);

    /// <summary>Advances the fake clock one second at a time until the pending move completes.</summary>
    private async Task<bool> Next(IAsyncEnumerator<LiveUpdate> feed)
    {
        var move = feed.MoveNextAsync().AsTask();
        for (var i = 0; i < 50 && !move.IsCompleted; i++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
            _time.Advance(LiveFeed.PollInterval);
        }

        return await move.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Frames_are_delivered_after_a_poll_interval()
    {
        _reader.Add(7, new Frame(_t0.AddSeconds(1), [1f]));
        await using var feed = Feed().FollowAsync(7, _t0, CancellationToken.None).GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await Next(feed)).ShouldBeTrue();
        feed.Current.ShouldBeOfType<FramesAppended>();
    }

    [Fact]
    public async Task A_failed_read_does_not_end_the_feed()
    {
        _reader.Add(7, new Frame(_t0.AddSeconds(1), [1f]));
        _reader.FailuresLeft = 1;
        await using var feed = Feed().FollowAsync(7, _t0, CancellationToken.None).GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await Next(feed)).ShouldBeTrue();
        feed.Current.ShouldBeOfType<FramesAppended>();
        _reader.Reads.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Cancellation_ends_the_feed()
    {
        using var cancellation = new CancellationTokenSource();
        await using var feed = Feed().FollowAsync(7, _t0, cancellation.Token).GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var move = feed.MoveNextAsync().AsTask();
        await cancellation.CancelAsync();

        (await move.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}

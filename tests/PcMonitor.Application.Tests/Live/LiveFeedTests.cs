using Microsoft.Extensions.Logging.Abstractions;
using PcMonitor.Application.Live;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Tests.Live;

public sealed class LiveFeedTests : LiveTestClock
{
    private IAsyncEnumerator<LiveUpdate> Start(CancellationToken cancellationToken = default) =>
        new LiveFeed(Reader, Time, NullLogger<LiveFeed>.Instance).FollowAsync(7, T0, cancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

    /// <summary>One frame one second after the start, already written.</summary>
    private void OneFrame() => Reader.Add(7, new Frame(T0.AddSeconds(1), [1f]));

    /// <summary>Advances the fake clock one second at a time until the pending move completes.</summary>
    private async Task<bool> Next(IAsyncEnumerator<LiveUpdate> feed)
    {
        var move = feed.MoveNextAsync().AsTask();
        for (var i = 0; i < 50 && !move.IsCompleted; i++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
            Time.Advance(LiveFeed.PollInterval);
        }

        return await move.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Frames_are_delivered_after_a_poll_interval()
    {
        OneFrame();
        await using var feed = Start(TestContext.Current.CancellationToken);

        (await Next(feed)).ShouldBeTrue();
        feed.Current.ShouldBeOfType<FramesAppended>();
    }

    [Fact]
    public async Task A_failed_read_does_not_end_the_feed()
    {
        OneFrame();
        Reader.FailuresLeft = 1;
        await using var feed = Start(TestContext.Current.CancellationToken);

        (await Next(feed)).ShouldBeTrue();
        feed.Current.ShouldBeOfType<FramesAppended>();
        Reader.Reads.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Cancellation_ends_the_feed()
    {
        using var cancellation = new CancellationTokenSource();
        await using var feed = Start(cancellation.Token);

        var move = feed.MoveNextAsync().AsTask();
        await cancellation.CancelAsync();

        (await move.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}

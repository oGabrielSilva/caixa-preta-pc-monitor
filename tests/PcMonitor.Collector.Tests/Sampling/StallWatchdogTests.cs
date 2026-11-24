using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PcMonitor.Collector.Sampling;

namespace PcMonitor.Collector.Tests.Sampling;

public sealed class StallWatchdogTests
{
    private sealed class RecordingTerminator : IProcessTerminator
    {
        public List<string> Reasons { get; } = [];

        public void Terminate(string reason) => Reasons.Add(reason);
    }

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordingTerminator _terminator = new();

    private StallWatchdog Watchdog() =>
        new(TimeSpan.FromSeconds(10), _time, _terminator, NullLogger<StallWatchdog>.Instance);

    [Fact]
    public void Watchdog_terminates_when_no_beat_within_the_timeout()
    {
        var watchdog = Watchdog();
        watchdog.Beat();

        _time.Advance(TimeSpan.FromSeconds(11));
        watchdog.Check();

        _terminator.Reasons.ShouldHaveSingleItem().ShouldContain("10");
    }

    [Fact]
    public void Watchdog_stays_quiet_while_beats_arrive()
    {
        var watchdog = Watchdog();
        for (var i = 0; i < 30; i++)
        {
            watchdog.Beat();
            _time.Advance(TimeSpan.FromSeconds(1));
            watchdog.Check();
        }

        _terminator.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Watchdog_counts_from_creation_before_the_first_beat()
    {
        var watchdog = Watchdog();

        _time.Advance(TimeSpan.FromSeconds(11));
        watchdog.Check();

        _terminator.Reasons.ShouldHaveSingleItem();
    }
}

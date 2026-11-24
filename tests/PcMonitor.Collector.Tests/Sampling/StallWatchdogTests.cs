using Microsoft.Extensions.Logging.Abstractions;
using PcMonitor.Collector.Sampling;

namespace PcMonitor.Collector.Tests.Sampling;

public sealed class StallWatchdogTests
{
    private sealed class RecordingTerminator : IProcessTerminator
    {
        public List<string> Reasons { get; } = [];

        public void Terminate(string reason) => Reasons.Add(reason);
    }

    private readonly RecordingTerminator _terminator = new();

    // 10 missed checks after the first beat (StallTimeout 10 s, 1 check/s); 60 before it (StartupTimeout 60 s).
    private StallWatchdog Watchdog() => new(maxMissedChecks: 10, maxMissedChecksBeforeFirstBeat: 60, _terminator,
        NullLogger<StallWatchdog>.Instance);

    private static void Check(StallWatchdog watchdog, int times)
    {
        for (var i = 0; i < times; i++)
        {
            watchdog.Check();
        }
    }

    [Fact]
    public void Watchdog_terminates_after_more_missed_checks_than_allowed()
    {
        var watchdog = Watchdog();
        watchdog.Beat();

        Check(watchdog, 11);

        _terminator.Reasons.ShouldHaveSingleItem().ShouldContain("10");
    }

    [Fact]
    public void Watchdog_tolerates_exactly_the_allowed_missed_checks()
    {
        var watchdog = Watchdog();
        watchdog.Beat();

        Check(watchdog, 10);

        _terminator.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Watchdog_stays_quiet_while_beats_arrive()
    {
        var watchdog = Watchdog();
        for (var i = 0; i < 100; i++)
        {
            watchdog.Beat();
            watchdog.Check();
        }

        _terminator.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Watchdog_survives_a_long_sleep_between_checks()
    {
        // Sleep/hibernate stops both the sampler and the check timer: the hours asleep never become missed checks.
        var watchdog = Watchdog();
        watchdog.Beat();

        watchdog.Check();
        watchdog.Beat();

        _terminator.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Watchdog_gives_startup_a_longer_budget_before_the_first_beat()
    {
        var watchdog = Watchdog();

        Check(watchdog, 60);
        _terminator.Reasons.ShouldBeEmpty();

        watchdog.Check();
        _terminator.Reasons.ShouldHaveSingleItem();
    }

    [Fact]
    public void Watchdog_terminates_only_once()
    {
        var watchdog = Watchdog();
        watchdog.Beat();

        Check(watchdog, 30);

        _terminator.Reasons.ShouldHaveSingleItem();
    }
}

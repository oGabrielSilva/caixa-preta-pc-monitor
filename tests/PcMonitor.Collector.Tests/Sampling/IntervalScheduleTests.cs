using PcMonitor.Collector.Sampling;

namespace PcMonitor.Collector.Tests.Sampling;

public sealed class IntervalScheduleTests
{
    /// <summary>Monotonic timestamps under test control; the wall clock can be set to anything.</summary>
    private sealed class ManualClock : TimeProvider
    {
        public long Ticks { get; set; }

        public DateTimeOffset WallClock { get; set; } = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Ticks;

        public override DateTimeOffset GetUtcNow() => WallClock;

        public void Elapse(TimeSpan span) => Ticks += span.Ticks;
    }

    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(1);
    private readonly ManualClock _clock = new();

    [Fact]
    public void Delay_is_the_rest_of_the_interval_after_a_fast_cycle()
    {
        var schedule = new IntervalSchedule(_clock, _interval);

        _clock.Elapse(TimeSpan.FromMilliseconds(200));

        schedule.NextDelay().ShouldBe(TimeSpan.FromMilliseconds(800));
    }

    [Fact]
    public void Delay_is_zero_after_a_cycle_slower_than_the_interval_and_the_schedule_resyncs()
    {
        var schedule = new IntervalSchedule(_clock, _interval);

        _clock.Elapse(TimeSpan.FromSeconds(3));
        schedule.NextDelay().ShouldBe(TimeSpan.Zero);

        _clock.Elapse(TimeSpan.FromMilliseconds(100));
        schedule.NextDelay().ShouldBe(TimeSpan.FromMilliseconds(900));
    }

    [Fact]
    public void Wall_clock_going_back_does_not_lengthen_the_wait()
    {
        var schedule = new IntervalSchedule(_clock, _interval);

        _clock.WallClock -= TimeSpan.FromHours(1);
        _clock.Elapse(TimeSpan.FromMilliseconds(100));

        schedule.NextDelay().ShouldBe(TimeSpan.FromMilliseconds(900));
    }

    [Fact]
    public void Delay_never_exceeds_the_interval() =>
        new IntervalSchedule(_clock, _interval).NextDelay().ShouldBeLessThanOrEqualTo(_interval);
}

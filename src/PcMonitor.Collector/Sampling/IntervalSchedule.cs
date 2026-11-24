namespace PcMonitor.Collector.Sampling;

/// <summary>
/// Fixed-rate schedule on the monotonic clock: slow cycles do not accumulate drift, and wall-clock adjustments
/// (NTP, manual changes) never stretch or shrink the wait. The delay is always within [0, interval].
/// </summary>
internal sealed class IntervalSchedule(TimeProvider time, TimeSpan interval)
{
    private long _nextTimestamp = time.GetTimestamp();

    /// <summary>Advances the schedule by one interval and returns how long to wait before the next cycle.</summary>
    public TimeSpan NextDelay()
    {
        var intervalTicks = (long)(interval.TotalSeconds * time.TimestampFrequency);
        _nextTimestamp += intervalTicks;

        var now = time.GetTimestamp();
        var remaining = _nextTimestamp - now;
        if (remaining <= 0)
        {
            _nextTimestamp = now;
            return TimeSpan.Zero;
        }

        if (remaining > intervalTicks)
        {
            _nextTimestamp = now + intervalTicks;
            return interval;
        }

        return TimeSpan.FromSeconds((double)remaining / time.TimestampFrequency);
    }
}

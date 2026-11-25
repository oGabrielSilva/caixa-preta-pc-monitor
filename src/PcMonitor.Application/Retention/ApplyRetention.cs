namespace PcMonitor.Application.Retention;

/// <summary>
/// Summarises complete minutes and hours, then deletes what is older than each retention period. Summaries come first, so
/// nothing is deleted before it has a coarser copy.
/// </summary>
public sealed class ApplyRetention(IRetentionStore store, RetentionPolicy policy, TimeProvider time)
{
    public RetentionResult Run()
    {
        var now = time.GetUtcNow();
        var currentMinute = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero);
        var currentHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);

        var minutes = store.AggregateMinutesBefore(currentMinute);
        var hours = store.AggregateHoursBefore(currentHour);
        var frames = store.DeleteFramesBefore(now - policy.RawRetention, policy.IncidentWindow);
        var minuteAggregates = store.DeleteMinuteAggregatesBefore(now - policy.AggregateRetention);
        return new RetentionResult(minutes, hours, frames, minuteAggregates);
    }
}

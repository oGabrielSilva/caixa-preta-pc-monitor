namespace PcMonitor.Application.Retention;

/// <summary>Aggregates complete minutes, then deletes raw frames older than the retention period.</summary>
public sealed class ApplyRetention(IRetentionStore store, RetentionPolicy policy, TimeProvider time)
{
    public RetentionResult Run()
    {
        var now = time.GetUtcNow();
        var currentMinute = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero);

        var minutes = store.AggregateMinutesBefore(currentMinute);
        var deleted = store.DeleteFramesBefore(now - policy.RawRetention);
        return new RetentionResult(minutes, deleted);
    }
}

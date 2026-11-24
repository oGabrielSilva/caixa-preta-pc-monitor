namespace PcMonitor.Application.History;

/// <summary>Builds the history of one sensor over a period, in buckets sized for a readable chart.</summary>
public sealed class QueryHistory(IHistoryReader reader, TimeProvider time)
{
    public static (TimeSpan Span, TimeSpan Bucket) Dimensions(HistoryRange range) => range switch
    {
        HistoryRange.Hour => (TimeSpan.FromHours(1), TimeSpan.FromMinutes(1)),
        HistoryRange.Day => (TimeSpan.FromDays(1), TimeSpan.FromMinutes(15)),
        HistoryRange.Week => (TimeSpan.FromDays(7), TimeSpan.FromHours(1)),
        HistoryRange.TwoWeeks => (TimeSpan.FromDays(14), TimeSpan.FromHours(2)),
        _ => throw new ArgumentOutOfRangeException(nameof(range), range, null),
    };

    /// <summary>The history of <paramref name="stableKey"/>, or <c>null</c> when that sensor was never recorded.</summary>
    public HistoryResult? Run(string stableKey, HistoryRange range)
    {
        var sensor = reader.Sensor(stableKey);
        if (sensor is null)
        {
            return null;
        }

        var (span, bucket) = Dimensions(range);
        var to = time.GetUtcNow();
        var from = to - span;
        var bucketMs = (long)bucket.TotalMilliseconds;

        var buckets = reader.Minutes(stableKey, from, to)
            .GroupBy(m => m.Minute.ToUnixTimeMilliseconds() / bucketMs * bucketMs)
            .OrderBy(g => g.Key)
            .Select(g => new HistoryBucket(
                DateTimeOffset.FromUnixTimeMilliseconds(g.Key),
                g.Min(m => m.Min),
                g.Average(m => m.Avg),
                g.Max(m => m.Max)))
            .ToList();

        return new HistoryResult(sensor, from, to, bucket, buckets);
    }
}

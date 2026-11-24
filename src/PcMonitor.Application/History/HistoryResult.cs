namespace PcMonitor.Application.History;

public sealed record HistoryResult(
    HistorySensor Sensor,
    DateTimeOffset From,
    DateTimeOffset To,
    TimeSpan Bucket,
    IReadOnlyList<HistoryBucket> Buckets);

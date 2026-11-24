using PcMonitor.Application.History;
using PcMonitor.Dashboard.Api.Sensors;

namespace PcMonitor.Dashboard.Api.History;

/// <summary>One sensor over a period, in buckets. Buckets without readings (machine off) are absent.</summary>
/// <param name="Sensor">The sensor and how to judge it.</param>
/// <param name="From">Start of the period.</param>
/// <param name="To">End of the period (now).</param>
/// <param name="BucketSeconds">Width of each bucket.</param>
/// <param name="Buckets">Oldest first.</param>
public sealed record HistoryResponse(
    SensorResponse Sensor,
    DateTimeOffset From,
    DateTimeOffset To,
    int BucketSeconds,
    IReadOnlyList<BucketResponse> Buckets)
{
    public static HistoryResponse Of(HistoryResult result) => new(
        Describe(result.Sensor),
        result.From,
        result.To,
        (int)result.Bucket.TotalSeconds,
        [.. result.Buckets.Select(b => new BucketResponse(b.Start.ToUnixTimeMilliseconds(), b.Min, b.Avg, b.Max))]);

    public static SensorResponse Describe(HistorySensor sensor) =>
        SensorPresentation.Describe(sensor.StableKey, sensor.Name, sensor.Kind, sensor.Unit, sensor.Role);
}

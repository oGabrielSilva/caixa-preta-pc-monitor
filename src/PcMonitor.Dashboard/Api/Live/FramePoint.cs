using PcMonitor.Dashboard.Api.Sensors;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Dashboard.Api.Live;

/// <summary>One frame: <c>t</c> in Unix milliseconds and one value per sensor, in the order of the sensor list (<c>null</c> = no reading).</summary>
public sealed record FramePoint(long T, IReadOnlyList<float?> Values)
{
    public static FramePoint From(Frame frame) =>
        new(frame.Timestamp.ToUnixTimeMilliseconds(), [.. frame.Values.Select(SensorPresentation.Value)]);
}

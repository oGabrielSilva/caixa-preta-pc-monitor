namespace PcMonitor.Dashboard.Api.Sensors;

/// <summary>One reading: <c>t</c> in Unix milliseconds.</summary>
public sealed record PointResponse(long T, float V);

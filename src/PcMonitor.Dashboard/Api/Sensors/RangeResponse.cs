namespace PcMonitor.Dashboard.Api.Sensors;

/// <summary>The safe range of a reading, inclusive (ATX ±5 % for supply rails).</summary>
public sealed record RangeResponse(float Min, float Max);

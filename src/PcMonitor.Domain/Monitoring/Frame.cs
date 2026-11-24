namespace PcMonitor.Domain.Monitoring;

/// <summary>
/// All sensor values read at one instant, in catalog order. <see cref="float.NaN"/> marks a sensor without a value.
/// </summary>
public sealed record Frame(DateTimeOffset Timestamp, IReadOnlyList<float> Values);

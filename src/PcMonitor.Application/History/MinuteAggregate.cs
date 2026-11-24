namespace PcMonitor.Application.History;

/// <summary>Minimum, mean and maximum of one sensor over one minute.</summary>
public sealed record MinuteAggregate(DateTimeOffset Minute, double Min, double Avg, double Max);

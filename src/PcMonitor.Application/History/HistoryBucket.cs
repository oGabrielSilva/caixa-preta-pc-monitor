namespace PcMonitor.Application.History;

/// <summary>One point of the history chart, starting at <see cref="Start"/>.</summary>
public sealed record HistoryBucket(DateTimeOffset Start, double Min, double Avg, double Max);

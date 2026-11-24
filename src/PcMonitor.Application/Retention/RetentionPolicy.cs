namespace PcMonitor.Application.Retention;

/// <summary>How long raw frames are kept. Aggregates are kept forever.</summary>
public sealed record RetentionPolicy(TimeSpan RawRetention);

/// <summary>Outcome of one retention run.</summary>
public sealed record RetentionResult(int MinutesAggregated, int FramesDeleted);

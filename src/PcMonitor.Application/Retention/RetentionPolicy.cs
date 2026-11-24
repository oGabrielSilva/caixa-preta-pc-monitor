namespace PcMonitor.Application.Retention;

/// <summary>
/// How long raw frames are kept (aggregates are kept forever). The <paramref name="IncidentWindow"/> before the end of each
/// incident's telemetry is never deleted: it is the evidence of what the machine was doing when it went down.
/// </summary>
public sealed record RetentionPolicy(TimeSpan RawRetention, TimeSpan IncidentWindow)
{
    public static readonly TimeSpan DefaultIncidentWindow = TimeSpan.FromSeconds(120);
}

/// <summary>Outcome of one retention run.</summary>
public sealed record RetentionResult(int MinutesAggregated, int FramesDeleted);

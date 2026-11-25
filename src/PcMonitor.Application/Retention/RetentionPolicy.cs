using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Application.Retention;

/// <summary>
/// How long each level of detail is kept: raw frames for <paramref name="RawRetention"/>, 1-minute aggregates for
/// <paramref name="AggregateRetention"/>, hourly aggregates forever. The <paramref name="IncidentWindow"/> before the end
/// of each incident's telemetry is never deleted: it is the evidence of what the machine was doing when it went down.
/// </summary>
public sealed record RetentionPolicy(TimeSpan RawRetention, TimeSpan AggregateRetention, TimeSpan IncidentWindow)
{
    /// <summary>The diagnosis window: retention keeps exactly what the rules read.</summary>
    public static readonly TimeSpan DefaultIncidentWindow = DiagnosisThresholds.Window;
}

/// <summary>Outcome of one retention run.</summary>
public sealed record RetentionResult(int MinutesAggregated, int HoursAggregated, int FramesDeleted, int MinuteAggregatesDeleted);

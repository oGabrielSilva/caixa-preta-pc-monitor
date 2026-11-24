using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Live;

/// <summary>A collector session as the dashboard shows it.</summary>
public sealed record SessionView(
    long Id,
    DateTimeOffset StartedAt,
    SensorSourceStatus SourceStatus,
    string? BoardProduct,
    DateTimeOffset? EndedAt,
    EndReason? EndReason);

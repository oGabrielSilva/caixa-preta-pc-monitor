using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Dashboard.Api.Live;

/// <summary>The collector session being shown.</summary>
/// <param name="StartedAt">When the collector started recording.</param>
/// <param name="SourceStatus">Whether the sensors could be read (<c>Ok</c>, or why not).</param>
/// <param name="BoardProduct">Motherboard model, when known.</param>
public sealed record SessionResponse(DateTimeOffset StartedAt, SensorSourceStatus SourceStatus, string? BoardProduct);

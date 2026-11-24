using PcMonitor.Application.Storage;

namespace PcMonitor.Dashboard.Api.Health;

/// <summary>Liveness of the dashboard, the version that is running and whether the database can be read.</summary>
/// <param name="Status">Always <c>ok</c> when the dashboard answers.</param>
/// <param name="Version">Semantic version of the dashboard.</param>
/// <param name="Storage">State of the monitoring database; data endpoints answer 503 unless it is <c>Ready</c>.</param>
public sealed record HealthResponse(string Status, string Version, StorageState Storage);

namespace PcMonitor.Dashboard.Api.Health;

/// <summary>Liveness of the dashboard and the version that is running.</summary>
/// <param name="Status">Always <c>ok</c> when the dashboard answers.</param>
/// <param name="Version">Semantic version of the dashboard.</param>
public sealed record HealthResponse(string Status, string Version);

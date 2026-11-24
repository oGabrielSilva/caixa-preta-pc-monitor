namespace PcMonitor.Application.Incidents;

/// <summary>Outcome of one incident detection run.</summary>
public sealed record DetectionResult(int Incidents, int WithTelemetry, int AbnormalSessions);

using PcMonitor.Application.Incidents;

namespace PcMonitor.Collector.Hosting;

/// <summary>
/// Runs incident detection once at start-up, before the new monitoring session opens. A failure is logged and never
/// prevents sampling: recording the next crash matters more than importing past ones.
/// </summary>
internal sealed partial class StartupIncidentDetection(DetectIncidents detectIncidents, ILogger<StartupIncidentDetection> logger)
{
    /// <returns>The detection result, or <c>null</c> when detection failed.</returns>
    public DetectionResult? Run()
    {
        try
        {
            var result = detectIncidents.Run();
            LogDetected(logger, result.Incidents, result.WithTelemetry, result.AbnormalSessions);
            return result;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogDetectionFailed(logger, exception);
            return null;
        }
    }

    [LoggerMessage(EventId = 1200, Level = LogLevel.Information,
        Message = "Incident detection: {Incidents} new incidents ({WithTelemetry} with telemetry), {Abnormal} abnormal sessions.")]
    private static partial void LogDetected(ILogger logger, int incidents, int withTelemetry, int abnormal);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Error, Message = "Incident detection failed; sampling starts anyway.")]
    private static partial void LogDetectionFailed(ILogger logger, Exception exception);
}

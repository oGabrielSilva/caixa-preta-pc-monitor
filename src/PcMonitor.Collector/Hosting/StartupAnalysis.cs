using PcMonitor.Application.Diagnosis;
using PcMonitor.Application.Incidents;

namespace PcMonitor.Collector.Hosting;

/// <summary>Outcome of the start-up analysis; <c>null</c> marks a step that failed (and was logged).</summary>
internal sealed record StartupAnalysisResult(DetectionResult? Detection, int? Diagnosed);

/// <summary>
/// Runs once at start-up, before the new monitoring session opens: detect incidents, then diagnose every incident that
/// needs it. Each step is isolated: a failure is logged and never blocks the other step or sampling.
/// </summary>
internal sealed partial class StartupAnalysis(
    DetectIncidents detectIncidents, DiagnoseIncidents diagnoseIncidents, ILogger<StartupAnalysis> logger)
{
    public StartupAnalysisResult Run()
    {
        var detection = Attempt(() => detectIncidents.Run(), "incident detection");
        if (detection is not null)
        {
            LogDetected(logger, detection.Incidents, detection.WithTelemetry, detection.AbnormalSessions);
        }

        var diagnosed = Attempt<int?>(() => diagnoseIncidents.Run(), "diagnosis");
        if (diagnosed is not null)
        {
            LogDiagnosed(logger, diagnosed.Value);
        }

        return new StartupAnalysisResult(detection, diagnosed);
    }

    private T? Attempt<T>(Func<T> step, string name)
    {
        try
        {
            return step();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogStepFailed(logger, exception, name);
            return default;
        }
    }

    [LoggerMessage(EventId = 1200, Level = LogLevel.Information,
        Message = "Incident detection: {Incidents} new incidents ({WithTelemetry} with telemetry), {Abnormal} abnormal sessions.")]
    private static partial void LogDetected(ILogger logger, int incidents, int withTelemetry, int abnormal);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Information, Message = "Diagnosis: {Count} incidents diagnosed.")]
    private static partial void LogDiagnosed(ILogger logger, int count);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Error, Message = "Start-up {Step} failed; sampling starts anyway.")]
    private static partial void LogStepFailed(ILogger logger, Exception exception, string step);
}

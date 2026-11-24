using Microsoft.Extensions.Options;
using PcMonitor.Application.Retention;
using PcMonitor.Collector.Configuration;

namespace PcMonitor.Collector.Retention;

/// <summary>Runs retention (aggregation, then deletion of old frames) periodically. Failures are logged, never fatal.</summary>
internal sealed partial class RetentionWorker(
    ApplyRetention retention,
    IOptions<CollectorOptions> options,
    TimeProvider time,
    ILogger<RetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Leave the host's start-up path before the first (synchronous, possibly long) run.
        await Task.Yield();
        using var timer = new PeriodicTimer(options.Value.RetentionInterval, time);
        try
        {
            do
            {
                RunOnce();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is stopping.
        }
    }

    private void RunOnce()
    {
        try
        {
            var result = retention.Run();
            LogRetentionRan(logger, result.MinutesAggregated, result.FramesDeleted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogRetentionFailed(logger, exception);
        }
    }

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Retention: {Minutes} minutes aggregated, {Frames} frames deleted.")]
    private static partial void LogRetentionRan(ILogger logger, int minutes, int frames);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Error, Message = "Retention failed; it will run again on the next interval.")]
    private static partial void LogRetentionFailed(ILogger logger, Exception exception);
}

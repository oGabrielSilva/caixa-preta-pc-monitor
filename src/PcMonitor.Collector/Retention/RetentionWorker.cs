using Microsoft.Extensions.Options;
using PcMonitor.Application.Retention;
using PcMonitor.Collector.Configuration;

namespace PcMonitor.Collector.Retention;

/// <summary>Runs retention (minute and hourly summaries, then deletions) periodically. Failures are logged, never fatal.</summary>
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
            LogRetentionRan(logger, result.MinutesAggregated, result.HoursAggregated, result.FramesDeleted, result.MinuteAggregatesDeleted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogRetentionFailed(logger, exception);
        }
    }

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information,
        Message = "Retention: {Minutes} minutes and {Hours} hourly rows summarised, {Frames} frames and {MinuteAggregates} minute aggregates deleted.")]
    private static partial void LogRetentionRan(ILogger logger, int minutes, int hours, int frames, int minuteAggregates);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Error, Message = "Retention failed; it will run again on the next interval.")]
    private static partial void LogRetentionFailed(ILogger logger, Exception exception);
}

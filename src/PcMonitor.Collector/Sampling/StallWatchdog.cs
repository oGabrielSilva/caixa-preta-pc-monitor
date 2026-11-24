namespace PcMonitor.Collector.Sampling;

/// <summary>
/// Detects a sampler stuck inside a hardware read (LibreHardwareMonitor reads cannot be cancelled) and terminates
/// the process so the service restarts, instead of silently recording nothing.
/// </summary>
/// <remarks>
/// Counts periodic checks that saw no beat instead of comparing clock times. Timers do not run while the machine
/// sleeps or hibernates, so time spent asleep never looks like a stall, and clock adjustments are irrelevant.
/// </remarks>
internal sealed partial class StallWatchdog(
    int maxMissedChecks,
    int maxMissedChecksBeforeFirstBeat,
    IProcessTerminator terminator,
    ILogger<StallWatchdog> logger)
{
    private int _missedChecks;
    private int _beaten;
    private int _terminated;

    /// <summary>Called by the sampler after every cycle (and on resume).</summary>
    public void Beat()
    {
        Volatile.Write(ref _beaten, 1);
        Interlocked.Exchange(ref _missedChecks, 0);
    }

    /// <summary>Called periodically from another thread.</summary>
    public void Check()
    {
        var limit = Volatile.Read(ref _beaten) == 1 ? maxMissedChecks : maxMissedChecksBeforeFirstBeat;
        var missed = Interlocked.Increment(ref _missedChecks);
        if (missed <= limit || Interlocked.Exchange(ref _terminated, 1) == 1)
        {
            return;
        }

        LogSamplerStalled(logger, missed);
        terminator.Terminate($"PcMonitor sampler stalled: no cycle completed in {limit} consecutive checks.");
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Critical,
        Message = "Sampler stalled: {MissedChecks} consecutive checks without a completed cycle; terminating so the service restarts.")]
    private static partial void LogSamplerStalled(ILogger logger, int missedChecks);
}

namespace PcMonitor.Collector.Sampling;

/// <summary>
/// Detects a sampler stuck inside a hardware read (LibreHardwareMonitor reads cannot be cancelled) and terminates
/// the process so the service restarts, instead of silently recording nothing.
/// </summary>
internal sealed partial class StallWatchdog(TimeSpan timeout, TimeProvider time, IProcessTerminator terminator, ILogger<StallWatchdog> logger)
{
    private long _lastBeatTicks = time.GetUtcNow().UtcTicks;

    /// <summary>Called by the sampler after every cycle.</summary>
    public void Beat() => Interlocked.Exchange(ref _lastBeatTicks, time.GetUtcNow().UtcTicks);

    /// <summary>Called periodically from another thread.</summary>
    public void Check()
    {
        var silence = time.GetUtcNow() - new DateTimeOffset(Interlocked.Read(ref _lastBeatTicks), TimeSpan.Zero);
        if (silence <= timeout)
        {
            return;
        }

        var reason = $"PcMonitor sampler stalled: no cycle completed in {timeout.TotalSeconds:0} s.";
        LogSamplerStalled(logger, silence);
        terminator.Terminate(reason);
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Critical,
        Message = "Sampler stalled for {Silence}; terminating so the service restarts.")]
    private static partial void LogSamplerStalled(ILogger logger, TimeSpan silence);
}

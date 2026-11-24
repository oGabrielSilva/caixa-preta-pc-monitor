namespace PcMonitor.Collector.Sampling;

/// <summary>Starts the sampler thread and checks the watchdog every second on the host's side.</summary>
internal sealed class SamplingWorker(SamplerThread sampler, StallWatchdog watchdog, TimeProvider time) : BackgroundService
{
    /// <summary>How often the watchdog is checked; its limits are expressed in checks of this length.</summary>
    public static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan _stopGrace = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        sampler.Start();
        using var timer = new PeriodicTimer(CheckInterval, time);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                watchdog.Check();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is stopping.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        sampler.Stop(_stopGrace);
    }
}

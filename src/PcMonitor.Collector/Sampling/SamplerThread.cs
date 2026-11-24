using Microsoft.Extensions.Options;
using PcMonitor.Application.Monitoring;
using PcMonitor.Collector.Configuration;
using PcMonitor.Collector.Hosting;

namespace PcMonitor.Collector.Sampling;

/// <summary>
/// Runs the monitoring recorder on a dedicated thread at a fixed rate. Hardware reads block, so they never run on
/// the host's thread pool; the <see cref="StallWatchdog"/> watches this thread from outside.
/// </summary>
internal sealed partial class SamplerThread(
    MonitoringRecorder recorder,
    ServiceSignals signals,
    StallWatchdog watchdog,
    IProcessTerminator terminator,
    TimeProvider time,
    IOptions<CollectorOptions> options,
    ILogger<SamplerThread> logger) : IDisposable
{
    private readonly CancellationTokenSource _stopping = new();
    private Thread? _thread;

    public void Start()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "PcMonitor.Sampler" };
        _thread.Start();
    }

    /// <summary>Asks the loop to end the session and waits for it, up to <paramref name="timeout"/>.</summary>
    public bool Stop(TimeSpan timeout)
    {
        _stopping.Cancel();
        return _thread?.Join(timeout) ?? true;
    }

    public void Dispose() => _stopping.Dispose();

    private void Run()
    {
        try
        {
            recorder.Start();
            watchdog.Beat();
            Loop(options.Value.SampleInterval, _stopping.Token);
            recorder.Stop(signals.StopReason);
        }
        catch (Exception exception)
        {
            LogSamplerFailed(logger, exception);
            terminator.Terminate("PcMonitor sampler failed: " + exception.Message);
        }
    }

    private void Loop(TimeSpan interval, CancellationToken stopping)
    {
        var schedule = new IntervalSchedule(time, interval);
        while (!stopping.IsCancellationRequested)
        {
            HandleSignals();
            recorder.RecordOnce();
            watchdog.Beat();
            stopping.WaitHandle.WaitOne(schedule.NextDelay());
        }
    }

    private void HandleSignals()
    {
        while (signals.TryDequeue(out var received))
        {
            switch (received.Signal)
            {
                case ServiceSignal.Suspend:
                    recorder.Suspend(received.ReceivedAt);
                    break;
                case ServiceSignal.Resume:
                    recorder.Resume();
                    watchdog.Beat();
                    break;
                case ServiceSignal.Shutdown:
                    // Handled by the stop path through ServiceSignals.StopReason.
                    break;
            }
        }
    }

    [LoggerMessage(EventId = 1002, Level = LogLevel.Critical, Message = "Sampler failed; terminating so the service restarts.")]
    private static partial void LogSamplerFailed(ILogger logger, Exception exception);
}

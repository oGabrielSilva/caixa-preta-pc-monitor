using System.Collections.Concurrent;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Collector.Hosting;

/// <summary>Windows events the sampler must react to.</summary>
internal enum ServiceSignal
{
    Shutdown,
    Suspend,
    Resume,
}

/// <summary>
/// Hands Windows service events from the service control thread to the sampler thread, and remembers why the
/// service is stopping.
/// </summary>
internal sealed class ServiceSignals
{
    private readonly ConcurrentQueue<ServiceSignal> _queue = new();
    private volatile bool _shutdown;

    /// <summary><see cref="EndReason.SystemShutdown"/> once Windows announced a shutdown; otherwise a plain stop.</summary>
    public EndReason StopReason => _shutdown ? EndReason.SystemShutdown : EndReason.ServiceStopped;

    public void Enqueue(ServiceSignal signal)
    {
        if (signal == ServiceSignal.Shutdown)
        {
            _shutdown = true;
        }

        _queue.Enqueue(signal);
    }

    public bool TryDequeue(out ServiceSignal signal) => _queue.TryDequeue(out signal);
}

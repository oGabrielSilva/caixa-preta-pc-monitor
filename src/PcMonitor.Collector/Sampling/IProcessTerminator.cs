namespace PcMonitor.Collector.Sampling;

/// <summary>Ends the process immediately so the Windows service recovery actions restart it.</summary>
internal interface IProcessTerminator
{
    void Terminate(string reason);
}

/// <summary>Uses <see cref="Environment.FailFast(string)"/>: no graceful stop, the service control manager sees a crash.</summary>
internal sealed class FailFastTerminator : IProcessTerminator
{
    public void Terminate(string reason) => Environment.FailFast(reason);
}

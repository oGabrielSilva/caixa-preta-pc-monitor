using System.ServiceProcess;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;

namespace PcMonitor.Collector.Hosting;

/// <summary>
/// Windows service lifetime that tells shutdown apart from a plain stop and reports sleep/resume.
/// The default <see cref="WindowsServiceLifetime"/> handles both stop and shutdown as a stop.
/// </summary>
internal sealed class PcMonitorServiceLifetime : WindowsServiceLifetime
{
    private readonly ServiceSignals _signals;

    public PcMonitorServiceLifetime(
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        ILoggerFactory loggerFactory,
        IOptions<HostOptions> optionsAccessor,
        IOptions<WindowsServiceLifetimeOptions> windowsServiceOptionsAccessor,
        ServiceSignals signals)
        : base(environment, applicationLifetime, loggerFactory, optionsAccessor, windowsServiceOptionsAccessor)
    {
        _signals = signals;
        CanShutdown = true;
        CanHandlePowerEvent = true;
    }

    protected override void OnShutdown()
    {
        _signals.Enqueue(ServiceSignal.Shutdown);
        base.OnShutdown();
    }

    protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
    {
        switch (powerStatus)
        {
            case PowerBroadcastStatus.Suspend:
                _signals.Enqueue(ServiceSignal.Suspend);
                break;
            case PowerBroadcastStatus.ResumeSuspend:
            case PowerBroadcastStatus.ResumeAutomatic:
                _signals.Enqueue(ServiceSignal.Resume);
                break;
        }

        return base.OnPowerEvent(powerStatus);
    }
}

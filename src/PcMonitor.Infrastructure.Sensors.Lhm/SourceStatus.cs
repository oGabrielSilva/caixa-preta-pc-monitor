using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Infrastructure.Sensors.Lhm;

/// <summary>Decides whether driver-backed sensors can be trusted.</summary>
public static class SourceStatus
{
    /// <summary>
    /// Driver-backed values are only real when PawnIO is installed <em>and</em> the process is elevated; otherwise
    /// LibreHardwareMonitor silently returns zeros or fixed placeholders.
    /// </summary>
    /// <returns>The status to record and whether driver-backed sensors may be recorded.</returns>
    public static (SensorSourceStatus Status, bool DriverUsable) Resolve(bool driverInstalled, bool privileged, bool superIoFound)
    {
        var driverUsable = driverInstalled && privileged;
        var status = !driverUsable ? SensorSourceStatus.DriverMissing
            : superIoFound ? SensorSourceStatus.Ok
            : SensorSourceStatus.BoardNotDetected;
        return (status, driverUsable);
    }
}

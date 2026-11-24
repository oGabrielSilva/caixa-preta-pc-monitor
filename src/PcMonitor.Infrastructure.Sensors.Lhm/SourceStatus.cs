using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Infrastructure.Sensors.Lhm;

/// <summary>Decides whether driver-backed sensors can be trusted.</summary>
public static class SourceStatus
{
    /// <summary>
    /// LibreHardwareMonitor never reports a driver problem: it silently returns zeros or fixed placeholders. Driver-backed
    /// values are trusted only when PawnIO is installed and running, the process is elevated, and the board's sensor
    /// chip was found (the proof that the driver actually reads hardware).
    /// </summary>
    /// <returns>The status to record and whether driver-backed sensors may be recorded.</returns>
    public static (SensorSourceStatus Status, bool DriverUsable) Resolve(
        bool driverInstalled, bool driverRunning, bool privileged, bool superIoFound)
    {
        if (!driverInstalled || !driverRunning || !privileged)
        {
            return (SensorSourceStatus.DriverMissing, false);
        }

        return superIoFound ? (SensorSourceStatus.Ok, true) : (SensorSourceStatus.BoardNotDetected, false);
    }
}

namespace PcMonitor.Domain.Monitoring;

/// <summary>Whether hardware sensors can be trusted for a session.</summary>
public enum SensorSourceStatus
{
    /// <summary>Driver loaded and the board's sensor chip was found.</summary>
    Ok,

    /// <summary>The low-level driver is not available; only driver-independent sensors are recorded.</summary>
    DriverMissing,

    /// <summary>The driver is available but no board sensor chip was found.</summary>
    BoardNotDetected,
}

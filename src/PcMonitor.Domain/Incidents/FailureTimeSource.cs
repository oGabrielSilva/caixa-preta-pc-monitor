namespace PcMonitor.Domain.Incidents;

/// <summary>Where the estimated failure time of an incident comes from, from most to least precise.</summary>
public enum FailureTimeSource
{
    /// <summary>Last frame recorded by the collector, which was running until the failure (within about a second).</summary>
    LastFrame,

    /// <summary>
    /// Last frame before the machine went to sleep; the failure happened later, while asleep or waking up.
    /// A lower bound, and the telemetry does not show the moment of the failure.
    /// </summary>
    LastFrameBeforeSleep,

    /// <summary>
    /// Last "alive" heartbeat Windows wrote in event 6008 (every ~20 min): a lower bound that can be minutes early.
    /// Also used when the collector had already stopped recording before the failure.
    /// </summary>
    LastAliveEventLog,

    /// <summary>No estimate available.</summary>
    Unknown,
}

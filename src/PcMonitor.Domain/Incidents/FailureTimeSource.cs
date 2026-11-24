namespace PcMonitor.Domain.Incidents;

/// <summary>Where the estimated failure time of an incident comes from, from most to least precise.</summary>
public enum FailureTimeSource
{
    /// <summary>Last frame recorded by the collector (within about a second).</summary>
    LastFrame,

    /// <summary>Last "alive" time Windows wrote in event 6008 (can be minutes before the failure).</summary>
    LastAliveEventLog,

    /// <summary>No estimate available.</summary>
    Unknown,
}

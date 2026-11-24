namespace PcMonitor.Domain.Monitoring;

/// <summary>Why a monitoring session or segment ended.</summary>
public enum EndReason
{
    /// <summary>The collector service was stopped.</summary>
    ServiceStopped,

    /// <summary>Windows shut down and notified the service.</summary>
    SystemShutdown,

    /// <summary>The machine went to sleep or hibernation.</summary>
    Suspended,

    /// <summary>Samples stopped arriving for longer than expected, with no power event.</summary>
    Gap,

    /// <summary>The collector ended without recording a reason and Windows reports no crash.</summary>
    Abnormal,
}

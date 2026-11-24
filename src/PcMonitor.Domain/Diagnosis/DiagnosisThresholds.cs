namespace PcMonitor.Domain.Diagnosis;

/// <summary>
/// Every threshold used by the rules, in one place. Rationale in docs/architecture/diagnosis.md. Changing any value
/// requires bumping <see cref="DiagnosisEngine.Version"/>.
/// </summary>
public static class DiagnosisThresholds
{
    /// <summary>Recorded time before the end of telemetry that the rules look at.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(120);

    /// <summary>"At the failure": the last seconds of the window.</summary>
    public static readonly TimeSpan End = TimeSpan.FromSeconds(10);

    /// <summary>Trend rules need at least this many frames.</summary>
    public const int MinFramesForTrend = 10;

    /// <summary>Load rules need at least this many frames in <see cref="End"/>.</summary>
    public const int MinFramesAtEnd = 5;

    public const float CpuCriticalCelsius = 90;
    public const float CpuHighCelsius = 80;
    public const float CpuNormalBelowCelsius = 75;
    public const float CpuRiseCelsius = 15;
    public static readonly TimeSpan CpuRiseSpan = TimeSpan.FromSeconds(30);

    public const float HighLoadPercent = 70;
    public const float IdleLoadPercent = 20;

    /// <summary>Back on (next boot logged) this soon after the last frame: it restarted by itself.</summary>
    public static readonly TimeSpan AutomaticRestart = TimeSpan.FromMinutes(3);

    /// <summary>Back on only after this long: it stayed off until someone turned it on.</summary>
    public static readonly TimeSpan StayedOff = TimeSpan.FromMinutes(15);

    /// <summary>A 6005 (event log start) this close to the last-alive time means no heartbeat after boot.</summary>
    public static readonly TimeSpan SameInstant = TimeSpan.FromSeconds(2);

    /// <summary>WHEA_UNCORRECTABLE_ERROR: a hardware error reported as a blue screen.</summary>
    public const long WheaBugcheck = 0x124;
}

namespace PcMonitor.Domain.Diagnosis;

/// <summary>
/// Every threshold used by the rules, in one place. Rationale in docs/architecture/diagnosis.md. Changing any value
/// requires bumping <see cref="DiagnosisEngine.Version"/>.
/// </summary>
public static class DiagnosisThresholds
{
    /// <summary>Recorded time before the end of telemetry that the rules look at (also kept by retention).</summary>
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(120);

    /// <summary>"At the failure": the last seconds of the window.</summary>
    public static readonly TimeSpan End = TimeSpan.FromSeconds(10);

    /// <summary>Trend rules need at least this many frames in the window.</summary>
    public const int MinFramesForTrend = 10;

    /// <summary>"At the failure" rules need at least this many frames in <see cref="End"/>.</summary>
    public const int MinFramesAtEnd = 5;

    /// <summary>A rail must read out of range at least this many times: one 1 Hz sample may be a glitch.</summary>
    public const int MinSamplesOutOfTolerance = 2;

    /// <summary>
    /// Well above Tjmax (95 °C on the Ryzen 7 8700G): the CPU should never get here; it is the thermal-trip zone.
    /// </summary>
    public const float CpuCriticalCelsius = 100;

    /// <summary>Tjmax: by design the CPU sits here under sustained load; only a sustained reading at the failure counts.</summary>
    public const float CpuAtLimitCelsius = 95;

    /// <summary>Below this at the failure, heat did not cause it.</summary>
    public const float CpuNormalBelowCelsius = 75;

    public const float HighLoadPercent = 70;
    public const float IdleLoadPercent = 20;

    /// <summary>Next boot logged this soon after the last frame: it came back quickly (by itself or by hand).</summary>
    public static readonly TimeSpan QuickReturn = TimeSpan.FromMinutes(3);

    /// <summary>Next boot logged only after this long: it stayed off for a while.</summary>
    public static readonly TimeSpan StayedOff = TimeSpan.FromMinutes(15);

    /// <summary>A 6005 (event log start) this close to the last-alive time means no heartbeat after boot.</summary>
    public static readonly TimeSpan SameInstant = TimeSpan.FromSeconds(2);

    /// <summary>WHEA_UNCORRECTABLE_ERROR: a hardware error reported as a blue screen.</summary>
    public const long WheaBugcheck = 0x124;
}

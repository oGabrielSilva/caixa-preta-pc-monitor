namespace PcMonitor.Domain.Diagnosis;

/// <summary>What a finding is for.</summary>
public enum FindingKind
{
    /// <summary>Weighs for or against a hypothesis.</summary>
    Evidence,

    /// <summary>A neutral fact worth showing (e.g. the crash happened soon after boot).</summary>
    Observation,

    /// <summary>Something that limits what this diagnosis can tell (e.g. no telemetry).</summary>
    Limitation,
}

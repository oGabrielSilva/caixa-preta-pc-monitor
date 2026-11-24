namespace PcMonitor.Domain.Diagnosis;

/// <summary>How much the top hypothesis stands out.</summary>
public enum Confidence
{
    /// <summary>No hypothesis has positive evidence.</summary>
    Inconclusive,

    Low,

    Medium,

    High,
}

namespace PcMonitor.Domain.Diagnosis;

/// <summary>Possible causes of an unexpected shutdown.</summary>
public enum Hypothesis
{
    /// <summary>The power supply stopped delivering stable power (protection trip, sag, failing unit).</summary>
    PowerSupply,

    /// <summary>The motherboard's power delivery or reset path: VRM, damaged traces, a short, a stuck reset line.</summary>
    BoardPowerDelivery,

    /// <summary>The CPU overheated and the hardware shut down to protect itself.</summary>
    Thermal,

    /// <summary>A driver or the operating system crashed.</summary>
    Software,

    /// <summary>The processor or memory reported hardware errors.</summary>
    CpuOrMemory,

    /// <summary>Someone held the power button: not a fault.</summary>
    ForcedPowerOff,
}

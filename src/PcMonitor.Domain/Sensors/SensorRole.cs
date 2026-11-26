namespace PcMonitor.Domain.Sensors;

/// <summary>
/// Meaning of a sensor for diagnosis, independent of how a board names it.
/// At most one sensor per catalog holds each role other than <see cref="None"/>.
/// </summary>
public enum SensorRole
{
    None,
    CpuTemperature,
    CpuPackagePower,
    CpuLoad,
    Vcore,
    Rail12V,
    Rail5V,
    Rail3V3,
    MemoryLoad,
}

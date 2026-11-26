namespace PcMonitor.Domain.Sensors;

/// <summary>Physical quantity a sensor measures.</summary>
public enum SensorKind
{
    Temperature,
    Voltage,
    Power,
    Load,
    Fan,

    /// <summary>A percentage level, such as the life left in a disk.</summary>
    Level,
}

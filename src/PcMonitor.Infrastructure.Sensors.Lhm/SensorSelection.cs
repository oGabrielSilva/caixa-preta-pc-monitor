using LibreHardwareMonitor.Hardware;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Sensors.Lhm.Profiles;

namespace PcMonitor.Infrastructure.Sensors.Lhm;

/// <summary>A LibreHardwareMonitor sensor chosen for recording, with its descriptor and value multiplier.</summary>
public sealed record SelectedSensor(ISensor Sensor, SensorDescriptor Descriptor, float Multiplier);

/// <summary>Decides which LibreHardwareMonitor sensors are recorded and how they are described.</summary>
public static class SensorSelection
{
    private static readonly Dictionary<SensorType, (SensorKind Kind, string Unit)> _recordedTypes = new()
    {
        [SensorType.Temperature] = (SensorKind.Temperature, "°C"),
        [SensorType.Voltage] = (SensorKind.Voltage, "V"),
        [SensorType.Power] = (SensorKind.Power, "W"),
        [SensorType.Load] = (SensorKind.Load, "%"),
        [SensorType.Fan] = (SensorKind.Fan, "RPM"),
        [SensorType.Level] = (SensorKind.Level, "%"),
    };

    /// <summary>
    /// Keeps temperature, voltage, power, load, fan and level sensors; without the driver only load sensors (the rest
    /// would be fake zeros); drops keys the profile excludes; applies the profile's names, roles and multipliers.
    /// </summary>
    public static IReadOnlyList<SelectedSensor> Select(IEnumerable<ISensor> sensors, BoardProfile? profile, bool driverAvailable)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        var known = profile?.Sensors.ToDictionary(s => s.Key, StringComparer.Ordinal) ?? [];

        return [.. sensors
            .Where(sensor => _recordedTypes.ContainsKey(sensor.SensorType))
            .Where(sensor => driverAvailable || sensor.SensorType == SensorType.Load)
            .Select(sensor => (Sensor: sensor, Key: sensor.Identifier.ToString()))
            .Where(s => profile is null || !profile.Exclude.Any(pattern => pattern.IsMatch(s.Key)))
            .OrderBy(s => s.Key, StringComparer.Ordinal)
            .Select(s =>
            {
                var (kind, unit) = _recordedTypes[s.Sensor.SensorType];
                return known.TryGetValue(s.Key, out var mapped)
                    ? new SelectedSensor(s.Sensor, new SensorDescriptor(s.Key, mapped.Name, kind, unit, mapped.Role), mapped.Multiplier)
                    : new SelectedSensor(s.Sensor, new SensorDescriptor(s.Key, s.Sensor.Name, kind, unit, SensorRole.None), 1);
            })];
    }
}

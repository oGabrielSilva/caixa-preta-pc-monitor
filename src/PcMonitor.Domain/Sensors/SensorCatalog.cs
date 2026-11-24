namespace PcMonitor.Domain.Sensors;

/// <summary>
/// Ordered, immutable set of sensors. The position of a sensor (its ordinal) is the position of its value
/// in every frame recorded with this catalog.
/// </summary>
public sealed class SensorCatalog
{
    private readonly SensorDescriptor[] _sensors;
    private readonly Dictionary<SensorRole, int> _ordinalByRole = [];

    public SensorCatalog(IEnumerable<SensorDescriptor> sensors)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        _sensors = [.. sensors];

        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var ordinal = 0; ordinal < _sensors.Length; ordinal++)
        {
            var sensor = _sensors[ordinal];
            if (!keys.Add(sensor.StableKey))
            {
                throw new ArgumentException($"Duplicate sensor key '{sensor.StableKey}'.", nameof(sensors));
            }

            if (sensor.Role != SensorRole.None && !_ordinalByRole.TryAdd(sensor.Role, ordinal))
            {
                throw new ArgumentException($"Role {sensor.Role} is assigned to more than one sensor.", nameof(sensors));
            }
        }
    }

    public IReadOnlyList<SensorDescriptor> Sensors => _sensors;

    public int Count => _sensors.Length;

    public SensorDescriptor this[int ordinal] => _sensors[ordinal];

    /// <summary>Position of the sensor holding <paramref name="role"/>, or <c>null</c> if no sensor has it.</summary>
    public int? OrdinalOf(SensorRole role) => _ordinalByRole.TryGetValue(role, out var ordinal) ? ordinal : null;
}

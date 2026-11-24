using PcMonitor.Domain.Sensors;

namespace PcMonitor.Domain.Diagnosis;

/// <summary>Sensor values recorded just before an incident, by role, oldest first. <c>NaN</c> values are dropped.</summary>
public sealed class TelemetryWindow
{
    private readonly Dictionary<SensorRole, IReadOnlyList<(DateTimeOffset At, float Value)>> _series;

    public TelemetryWindow(DateTimeOffset end, IReadOnlyDictionary<SensorRole, IReadOnlyList<(DateTimeOffset At, float Value)>> series)
    {
        ArgumentNullException.ThrowIfNull(series);
        End = end;
        _series = series
            .Select(s => (s.Key, Points: (IReadOnlyList<(DateTimeOffset At, float Value)>)[.. s.Value.Where(p => !float.IsNaN(p.Value)).OrderBy(p => p.At)]))
            .Where(s => s.Points.Count > 0)
            .ToDictionary(s => s.Key, s => s.Points);
    }

    /// <summary>Time of the last frame.</summary>
    public DateTimeOffset End { get; }

    public bool Has(SensorRole role) => _series.ContainsKey(role);

    public IReadOnlyList<(DateTimeOffset At, float Value)> Series(SensorRole role) => _series.GetValueOrDefault(role) ?? [];

    /// <summary>Values within <paramref name="span"/> before <see cref="End"/>.</summary>
    public IReadOnlyList<(DateTimeOffset At, float Value)> Tail(SensorRole role, TimeSpan span) =>
        [.. Series(role).Where(p => p.At > End - span)];
}

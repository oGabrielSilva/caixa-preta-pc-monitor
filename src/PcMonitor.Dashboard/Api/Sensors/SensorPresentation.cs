using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Dashboard.Api.Sensors;

/// <summary>
/// How readings are judged on screen. Ranges and limits are the diagnosis's own (<see cref="RailTolerance"/>,
/// <see cref="DiagnosisThresholds"/>), so the dashboard never disagrees with the diagnosis.
/// </summary>
internal static class SensorPresentation
{
    public static SensorResponse Describe(string key, string name, SensorKind kind, string unit, SensorRole role)
    {
        var storage = StorageMeasure.Of(key);
        var range = storage?.Kind == StorageMeasureKind.Life ? new RangeResponse(StorageMeasure.MinimumLifePercent, 100) : RangeOf(role);
        return new(key, name, kind, unit, role, range, LimitOf(role), CriticalOf(role),
            storage is null ? null : new StorageResponse(storage.Disk, storage.Kind));
    }

    public static SensorResponse Describe(SensorDescriptor sensor) =>
        Describe(sensor.StableKey, sensor.Name, sensor.Kind, sensor.Unit, sensor.Role);

    public static RangeResponse? RangeOf(SensorRole role) =>
        RailTolerance.Atx.FirstOrDefault(r => r.Role == role) is { } rail ? new RangeResponse(rail.Min, rail.Max) : null;

    public static float? LimitOf(SensorRole role) => role == SensorRole.CpuTemperature ? DiagnosisThresholds.CpuAtLimitCelsius : null;

    public static float? CriticalOf(SensorRole role) => role == SensorRole.CpuTemperature ? DiagnosisThresholds.CpuCriticalCelsius : null;

    /// <summary>The unit of a role's readings, for series that carry only the role.</summary>
    public static string UnitOf(SensorRole role) => role switch
    {
        SensorRole.CpuTemperature => "°C",
        SensorRole.CpuLoad => "%",
        SensorRole.CpuPackagePower => "W",
        SensorRole.Vcore or SensorRole.Rail12V or SensorRole.Rail5V or SensorRole.Rail3V3 => "V",
        _ => string.Empty,
    };

    /// <summary>A reading for JSON: <c>NaN</c> and infinities (no reading) become <c>null</c>.</summary>
    public static float? Value(float value) => float.IsFinite(value) ? value : null;
}

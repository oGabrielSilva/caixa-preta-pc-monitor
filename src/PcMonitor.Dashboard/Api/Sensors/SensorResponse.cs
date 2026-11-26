using PcMonitor.Domain.Sensors;

namespace PcMonitor.Dashboard.Api.Sensors;

/// <summary>A recorded sensor and how to judge its readings.</summary>
/// <param name="Key">Stable key of the sensor.</param>
/// <param name="Name">Name as recorded.</param>
/// <param name="Kind">What it measures.</param>
/// <param name="Unit">Unit of the readings.</param>
/// <param name="Role">Meaning on this board (<c>None</c> for sensors without one).</param>
/// <param name="Range">Safe range, for supply rails.</param>
/// <param name="Limit">Reading at which to pay attention (CPU at its design limit).</param>
/// <param name="Critical">Reading that is out of range (CPU shutdown zone).</param>
/// <param name="Storage">For a disk sensor, which disk and what it measures.</param>
public sealed record SensorResponse(
    string Key,
    string Name,
    SensorKind Kind,
    string Unit,
    SensorRole Role,
    RangeResponse? Range,
    float? Limit,
    float? Critical,
    StorageResponse? Storage);

/// <summary>A disk sensor: the disk (0-based) and what it measures.</summary>
public sealed record StorageResponse(int Disk, StorageMeasureKind Measure);

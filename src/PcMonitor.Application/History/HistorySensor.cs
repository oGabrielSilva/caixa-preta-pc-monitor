using PcMonitor.Domain.Sensors;

namespace PcMonitor.Application.History;

/// <summary>A sensor that has recorded data, with the name of its most recent segment.</summary>
public sealed record HistorySensor(string StableKey, string Name, SensorKind Kind, string Unit, SensorRole Role);

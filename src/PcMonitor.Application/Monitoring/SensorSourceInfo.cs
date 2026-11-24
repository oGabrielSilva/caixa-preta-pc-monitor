using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Application.Monitoring;

/// <summary>What a sensor source found when opened.</summary>
/// <param name="Status">Whether hardware sensors can be trusted.</param>
/// <param name="BoardProduct">Motherboard product name from SMBIOS, when known.</param>
/// <param name="Catalog">Sensors the source will report, in read order.</param>
public sealed record SensorSourceInfo(SensorSourceStatus Status, string? BoardProduct, SensorCatalog Catalog);

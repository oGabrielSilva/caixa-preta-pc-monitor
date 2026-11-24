namespace PcMonitor.Domain.Sensors;

/// <summary>Identity and meaning of one sensor.</summary>
/// <param name="StableKey">Identifier that stays the same across boots (e.g. <c>/lpc/nct6799d/0/voltage/4</c>).</param>
/// <param name="Name">Human-readable name (e.g. <c>+12V</c>).</param>
/// <param name="Kind">Quantity measured.</param>
/// <param name="Unit">Unit of the values (e.g. <c>V</c>, <c>°C</c>).</param>
/// <param name="Role">Meaning for diagnosis, or <see cref="SensorRole.None"/>.</param>
public sealed record SensorDescriptor(string StableKey, string Name, SensorKind Kind, string Unit, SensorRole Role);

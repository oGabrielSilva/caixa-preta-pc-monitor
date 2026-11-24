using PcMonitor.Domain.Sensors;

namespace PcMonitor.Domain.Diagnosis;

/// <summary>
/// ATX ±5 % limits of a supply rail, and the sag that counts as a droop. Droop thresholds sit above the sensor chip's
/// resolution after the divider (~0.1 V on +12 V, ~0.04 V on +5 V) so a single ADC step never looks like a sag.
/// </summary>
public sealed record RailTolerance(SensorRole Role, string Name, float Min, float Max, float? DroopVolts)
{
    public static readonly IReadOnlyList<RailTolerance> Atx =
    [
        new(SensorRole.Rail12V, "12V", 11.40f, 12.60f, DroopVolts: 0.2f),
        new(SensorRole.Rail5V, "5V", 4.75f, 5.25f, DroopVolts: 0.1f),
        new(SensorRole.Rail3V3, "3.3V", 3.135f, 3.465f, DroopVolts: null),
    ];

    public bool Contains(float volts) => volts >= Min && volts <= Max;

    /// <summary>How far outside the range a value is (0 inside).</summary>
    public float Excess(float volts) => volts < Min ? Min - volts : volts > Max ? volts - Max : 0;
}

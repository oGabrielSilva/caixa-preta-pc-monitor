using System.Text.RegularExpressions;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Infrastructure.Sensors.Lhm.Profiles;

/// <summary>
/// How to read one motherboard: which raw sensors to drop and which ones get a name, a role and a multiplier.
/// Multipliers undo the voltage dividers in front of the sensor chip (e.g. +12V is read as ~1 V × 12).
/// </summary>
public sealed record BoardProfile(string BoardProduct, IReadOnlyList<Regex> Exclude, IReadOnlyList<ProfileSensor> Sensors);

/// <summary>A sensor the profile knows. <paramref name="Key"/> is the LibreHardwareMonitor identifier.</summary>
public sealed record ProfileSensor(string Key, string Name, SensorRole Role, float Multiplier = 1);

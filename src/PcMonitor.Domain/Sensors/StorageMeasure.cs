using System.Text.RegularExpressions;

namespace PcMonitor.Domain.Sensors;

public enum StorageMeasureKind
{
    Temperature,

    /// <summary>The temperature at which the disk itself says to pay attention.</summary>
    WarningTemperature,

    /// <summary>The temperature the disk itself calls critical.</summary>
    CriticalTemperature,

    UsedSpace,

    /// <summary>Life left, in percent (100 = new).</summary>
    Life,
}

/// <summary>
/// What a disk sensor measures, recognised by its LibreHardwareMonitor key (NVMe drives). A machine can have several
/// disks, so these are not sensor roles (a role is unique per catalog).
/// </summary>
public sealed partial record StorageMeasure(int Disk, StorageMeasureKind Kind)
{
    /// <summary>Below this much life left, a disk is out of its safe range.</summary>
    public const float MinimumLifePercent = 10;

    private static readonly Dictionary<string, StorageMeasureKind> _byIndex = new(StringComparer.Ordinal)
    {
        ["temperature/0"] = StorageMeasureKind.Temperature,
        ["temperature/10"] = StorageMeasureKind.WarningTemperature,
        ["temperature/11"] = StorageMeasureKind.CriticalTemperature,
        ["load/30"] = StorageMeasureKind.UsedSpace,
        ["level/20"] = StorageMeasureKind.Life,
    };

    /// <summary>The measure of a disk sensor, or <c>null</c> for any other sensor.</summary>
    public static StorageMeasure? Of(string stableKey)
    {
        var match = NvmeKey().Match(stableKey);
        return match.Success && _byIndex.TryGetValue(match.Groups["sensor"].Value, out var kind)
            ? new StorageMeasure(int.Parse(match.Groups["disk"].Value, System.Globalization.CultureInfo.InvariantCulture), kind)
            : null;
    }

    [GeneratedRegex(@"^/nvme/(?<disk>\d+)/(?<sensor>[a-z]+/\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex NvmeKey();
}

using System.Globalization;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Infrastructure.Windows.EventLog;

/// <summary>Builds an <see cref="UnexpectedShutdown"/> from a Kernel-Power 41 event and the 6008 of the same boot.</summary>
public static class UnexpectedShutdownMapper
{
    public static UnexpectedShutdown Map(SystemEvent kernelPower41, SystemEvent? eventLog6008)
    {
        ArgumentNullException.ThrowIfNull(kernelPower41);
        var data = kernelPower41.Data;

        return new UnexpectedShutdown(
            kernelPower41.RecordId,
            kernelPower41.LoggedAt,
            BugcheckCode: Number(data, "BugcheckCode"),
            SleepState: (int)Number(data, "SleepInProgress"),
            PowerButtonPressedAt: FromFileTime(Number(data, "PowerButtonTimestamp")),
            LongPowerButtonPress: data.TryGetValue("LongPowerButtonPressDetected", out var longPress)
                && bool.TryParse(longPress, out var pressed) && pressed,
            LastAliveAt: LastAliveTime.FromBinary(eventLog6008?.Data.GetValueOrDefault("Binary")),
            data);
    }

    /// <summary>FILETIME (100 ns since 1601) as UTC; <c>null</c> for 0 or a value outside the valid range.</summary>
    private static DateTimeOffset? FromFileTime(long fileTime)
    {
        if (fileTime <= 0)
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromFileTime(fileTime).ToUniversalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static long Number(IReadOnlyDictionary<string, string> data, string name) =>
        data.TryGetValue(name, out var text) && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
}

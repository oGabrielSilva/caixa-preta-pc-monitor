using System.Buffers.Binary;

namespace PcMonitor.Infrastructure.Windows.EventLog;

/// <summary>
/// Reads the time from the binary part of event 6008 ("the previous system shutdown was unexpected"). Its text is
/// localized and full of invisible marks; the binary holds two <c>SYSTEMTIME</c> structures, local then UTC.
/// </summary>
public static class LastAliveTime
{
    private const int SystemTimeBytes = 16;

    /// <summary>The UTC <c>SYSTEMTIME</c> (bytes 16–31), or <c>null</c> when missing, short or invalid.</summary>
    public static DateTimeOffset? FromBinary(string? hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0 || hex.Length < 4 * SystemTimeBytes)
        {
            return null;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            return null;
        }

        int Field(int index) => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(SystemTimeBytes + (index * 2)));

        try
        {
            // SYSTEMTIME: year, month, day of week, day, hour, minute, second, milliseconds.
            return new DateTimeOffset(Field(0), Field(1), Field(3), Field(4), Field(5), Field(6), Field(7), TimeSpan.Zero);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}

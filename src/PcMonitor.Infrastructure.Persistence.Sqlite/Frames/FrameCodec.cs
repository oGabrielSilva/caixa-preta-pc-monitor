using System.Buffers.Binary;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Frames;

/// <summary>
/// The only place that knows the frame BLOB format: one little-endian <c>float32</c> per sensor, in catalog order.
/// </summary>
public static class FrameCodec
{
    private const int BytesPerValue = sizeof(float);

    public static byte[] Encode(IReadOnlyList<float> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var blob = new byte[values.Count * BytesPerValue];
        for (var i = 0; i < values.Count; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(blob.AsSpan(i * BytesPerValue), values[i]);
        }

        return blob;
    }

    public static float[] Decode(ReadOnlySpan<byte> blob)
    {
        if (blob.Length % BytesPerValue != 0)
        {
            throw new ArgumentException($"Frame blob length {blob.Length} is not a multiple of {BytesPerValue}.", nameof(blob));
        }

        var values = new float[blob.Length / BytesPerValue];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = BinaryPrimitives.ReadSingleLittleEndian(blob[(i * BytesPerValue)..]);
        }

        return values;
    }
}

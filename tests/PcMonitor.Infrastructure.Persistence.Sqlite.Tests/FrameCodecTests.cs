using PcMonitor.Infrastructure.Persistence.Sqlite.Frames;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class FrameCodecTests
{
    [Fact]
    public void Codec_round_trips_values_including_NaN()
    {
        float[] values = [1.5f, float.NaN, -3f, 12.192f];

        FrameCodec.Decode(FrameCodec.Encode(values)).ShouldBe(values);
    }

    [Fact]
    public void Codec_uses_four_bytes_per_value() => FrameCodec.Encode([1f, 2f, 3f]).Length.ShouldBe(12);

    [Fact]
    public void Codec_rejects_a_blob_whose_length_is_not_a_multiple_of_4() =>
        Should.Throw<ArgumentException>(() => FrameCodec.Decode(new byte[5]));
}

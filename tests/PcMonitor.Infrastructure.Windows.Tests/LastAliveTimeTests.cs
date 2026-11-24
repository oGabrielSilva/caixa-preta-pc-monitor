using PcMonitor.Infrastructure.Windows.EventLog;
using PcMonitor.Infrastructure.Windows.Tests.Support;

namespace PcMonitor.Infrastructure.Windows.Tests;

public sealed class LastAliveTimeTests
{
    [Fact]
    public void LastAlive_is_the_UTC_systemtime_in_the_6008_binary()
    {
        var binary = EventXml.Parse(Fixture.Read("eventlog-6008.xml")).Data["Binary"];

        LastAliveTime.FromBinary(binary).ShouldBe(new DateTimeOffset(2026, 11, 24, 16, 50, 34, 437, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EA070B00")]
    [InlineData("EA070B000200180010003200220000000000000000000000000000000000000")] // odd length
    [InlineData("EA070B00020018000D0032002200B501EA070D00020018001000320022000000")] // second month 13: invalid
    public void LastAlive_is_null_for_missing_short_or_invalid_binary(string? hex) =>
        LastAliveTime.FromBinary(hex).ShouldBeNull();
}

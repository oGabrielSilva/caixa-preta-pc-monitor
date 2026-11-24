using PcMonitor.Infrastructure.Windows.EventLog;
using PcMonitor.Infrastructure.Windows.Tests.Support;

namespace PcMonitor.Infrastructure.Windows.Tests;

public sealed class EventXmlTests
{
    [Fact]
    public void Parse_reads_provider_id_record_time_level_and_named_data()
    {
        var parsed = EventXml.Parse(Fixture.Read("kernel-power-41.xml"));

        parsed.Provider.ShouldBe("Microsoft-Windows-Kernel-Power");
        parsed.EventId.ShouldBe(41);
        parsed.RecordId.ShouldBe(402399);
        parsed.LoggedAt.ShouldBe(new DateTimeOffset(2026, 11, 24, 16, 54, 38, TimeSpan.Zero).AddTicks(9245715));
        parsed.Level.ShouldBe(1);
        parsed.Data["BugcheckCode"].ShouldBe("0");
        parsed.Data["LongPowerButtonPressDetected"].ShouldBe("false");
    }

    [Fact]
    public void Parse_keys_unnamed_data_by_position_and_keeps_the_binary()
    {
        var parsed = EventXml.Parse(Fixture.Read("eventlog-6008.xml"));

        parsed.Provider.ShouldBe("EventLog");
        parsed.EventId.ShouldBe(6008);
        parsed.Data["0"].ShouldBe("13:50:34");
        parsed.Data["Binary"].ShouldStartWith("EA070B00");
    }
}

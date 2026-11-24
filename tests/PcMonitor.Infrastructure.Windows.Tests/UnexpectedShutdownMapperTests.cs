using PcMonitor.Domain.Incidents;
using PcMonitor.Infrastructure.Windows.EventLog;
using PcMonitor.Infrastructure.Windows.Tests.Support;

namespace PcMonitor.Infrastructure.Windows.Tests;

public sealed class UnexpectedShutdownMapperTests
{
    private static readonly SystemEvent _kernelPower41 = EventXml.Parse(Fixture.Read("kernel-power-41.xml"));
    private static readonly SystemEvent _eventLog6008 = EventXml.Parse(Fixture.Read("eventlog-6008.xml"));

    [Fact]
    public void Mapper_builds_the_shutdown_from_41_and_its_6008()
    {
        var shutdown = UnexpectedShutdownMapper.Map(_kernelPower41, _eventLog6008);

        shutdown.RecordId.ShouldBe(402399);
        shutdown.LoggedAt.ShouldBe(_kernelPower41.LoggedAt);
        shutdown.BugcheckCode.ShouldBe(0);
        shutdown.SleepInProgress.ShouldBeFalse();
        shutdown.PowerButtonPressedAt.ShouldBeNull();
        shutdown.LongPowerButtonPress.ShouldBeFalse();
        shutdown.LastAliveAt.ShouldBe(new DateTimeOffset(2026, 11, 24, 16, 50, 34, 437, TimeSpan.Zero));
        shutdown.Data.ShouldBeSameAs(_kernelPower41.Data);
    }

    [Fact]
    public void Mapper_without_6008_has_no_last_alive_time() =>
        UnexpectedShutdownMapper.Map(_kernelPower41, eventLog6008: null).LastAliveAt.ShouldBeNull();

    [Fact]
    public void Mapper_reads_bugcheck_sleep_and_power_button_values()
    {
        var pressedAt = new DateTimeOffset(2026, 11, 24, 16, 0, 0, TimeSpan.Zero);
        var data = new Dictionary<string, string>(_kernelPower41.Data)
        {
            ["BugcheckCode"] = "156",
            ["SleepInProgress"] = "4",
            ["PowerButtonTimestamp"] = pressedAt.ToFileTime().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["LongPowerButtonPressDetected"] = "true",
        };

        var shutdown = UnexpectedShutdownMapper.Map(_kernelPower41 with { Data = data }, null);

        (shutdown.BugcheckCode, shutdown.SleepInProgress, shutdown.PowerButtonPressedAt, shutdown.LongPowerButtonPress)
            .ShouldBe((156L, true, pressedAt, true));
    }

    [Fact]
    public void Mapper_keeps_bugcheck_codes_above_the_int_range() =>
        UnexpectedShutdownMapper.Map(_kernelPower41 with { Data = new Dictionary<string, string> { ["BugcheckCode"] = "3221226010" } }, null)
            .BugcheckCode.ShouldBe(0xC000021AL);

    [Fact]
    public void Mapper_ignores_an_out_of_range_power_button_timestamp() =>
        UnexpectedShutdownMapper.Map(
                _kernelPower41 with { Data = new Dictionary<string, string> { ["PowerButtonTimestamp"] = long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                null)
            .PowerButtonPressedAt.ShouldBeNull();

    [Fact]
    public void Mapper_tolerates_missing_data() =>
        UnexpectedShutdownMapper.Map(_kernelPower41 with { Data = new Dictionary<string, string>() }, null).BugcheckCode.ShouldBe(0);
}

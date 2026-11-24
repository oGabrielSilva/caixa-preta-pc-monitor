using PcMonitor.Domain.Incidents;
using PcMonitor.Infrastructure.Windows.EventLog;

namespace PcMonitor.Infrastructure.Windows.Tests;

public sealed class ShutdownReportPairingTests
{
    private static readonly DateTimeOffset _ten = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    // At boot, the EventLog service writes the 6008 just before the Kernel-Power 41 in record order, a few seconds later in time.
    private static SystemEvent KernelPower41(long record, DateTimeOffset at) =>
        new("Microsoft-Windows-Kernel-Power", 41, record, at, 1, new Dictionary<string, string>());

    private static SystemEvent Report6008(long record, DateTimeOffset at) =>
        new("EventLog", 6008, record, at, 2, new Dictionary<string, string>());

    [Fact]
    public void Each_shutdown_gets_the_report_written_at_its_own_boot()
    {
        var l1 = KernelPower41(101, _ten.AddSeconds(2));
        var r1 = Report6008(100, _ten.AddSeconds(13));
        var l2 = KernelPower41(201, _ten.AddMinutes(30));
        var r2 = Report6008(200, _ten.AddMinutes(30).AddSeconds(11));

        ShutdownReportPairing.Pair([l2, l1], [r2, r1]).ShouldBe([(l1, r1), (l2, r2)]);
    }

    [Fact]
    public void Report_of_the_previous_boot_is_not_used_even_when_it_is_less_than_two_minutes_away()
    {
        // Crash again 16 s after the previous boot's EventLog started: boot loop.
        var l1 = KernelPower41(101, _ten.AddSeconds(2));
        var r1 = Report6008(100, _ten.AddSeconds(13));
        var l2 = KernelPower41(201, _ten.AddSeconds(47));
        var r2 = Report6008(200, _ten.AddSeconds(58));

        ShutdownReportPairing.Pair([l1, l2], [r1, r2])[1].ShouldBe((l2, r2));
    }

    [Fact]
    public void Report_written_minutes_after_a_slow_boot_is_still_paired()
    {
        var l1 = KernelPower41(101, _ten);
        var r1 = Report6008(100, _ten.AddMinutes(5)); // autochk after a dirty shutdown

        ShutdownReportPairing.Pair([l1], [r1]).ShouldBe([(l1, r1)]);
    }

    [Fact]
    public void Shutdown_without_a_report_at_its_boot_gets_none()
    {
        var r1 = Report6008(100, _ten.AddSeconds(13));
        var l1 = KernelPower41(101, _ten.AddSeconds(2));
        var l2 = KernelPower41(201, _ten.AddMinutes(30));

        ShutdownReportPairing.Pair([l1, l2], [r1]).ShouldBe([(l1, r1), (l2, null)]);
    }

    [Fact]
    public void Report_far_from_the_shutdown_in_time_is_not_paired()
    {
        var l1 = KernelPower41(101, _ten);
        var stale = Report6008(100, _ten.AddDays(-3));

        ShutdownReportPairing.Pair([l1], [stale]).ShouldBe([(l1, null)]);
    }
}

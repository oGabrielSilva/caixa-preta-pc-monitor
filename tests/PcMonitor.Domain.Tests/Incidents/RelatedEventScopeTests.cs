using PcMonitor.Domain.Incidents;

namespace PcMonitor.Domain.Tests.Incidents;

public sealed class RelatedEventScopeTests
{
    private static readonly DateTimeOffset _crash1 = new(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _crash2 = _crash1.AddMinutes(10);

    private static SystemEvent Event(DateTimeOffset at, string provider = "Microsoft-Windows-WHEA-Logger", int id = 18) =>
        new(provider, id, 1, at, 2, new Dictionary<string, string>());

    private static SystemEvent BootStart(DateTimeOffset at) => Event(at, "EventLog", 6005);

    [Theory]
    [InlineData(30, true)]   // boot-time events right after crash 1's 41: crash 1
    [InlineData(-300, true)] // before crash 1, no previous crash
    [InlineData(121, false)] // after crash 1's boot settled: belongs to the next crash
    public void Events_belong_to_the_first_crash_up_to_two_minutes_after_its_event(int secondsFromCrash1, bool belongs) =>
        RelatedEventScope.Belongs(Event(_crash1.AddSeconds(secondsFromCrash1)), _crash1, previousLoggedAt: null).ShouldBe(belongs);

    [Theory]
    [InlineData(30, false)]  // crash 1's boot-time events never belong to crash 2
    [InlineData(300, true)]
    [InlineData(600 + 120, true)]
    [InlineData(600 + 121, false)]
    public void Events_of_the_second_crash_start_after_the_first_crash_boot_settled(int secondsFromCrash1, bool belongs) =>
        RelatedEventScope.Belongs(Event(_crash1.AddSeconds(secondsFromCrash1)), _crash2, previousLoggedAt: _crash1).ShouldBe(belongs);

    [Fact]
    public void The_event_log_start_marks_the_session_the_next_crash_ended()
    {
        // 6005 is written seconds after crash 1's 41, at the boot that starts the session crash 2 ends.
        var bootStart = BootStart(_crash1.AddSeconds(11));

        RelatedEventScope.Belongs(bootStart, _crash1, previousLoggedAt: null).ShouldBeFalse();
        RelatedEventScope.Belongs(bootStart, _crash2, previousLoggedAt: _crash1).ShouldBeTrue();
    }

    [Fact]
    public void Query_window_reaches_back_to_the_previous_crash_and_two_minutes_after_this_one()
    {
        var (since, until) = RelatedEventScope.Window(_crash1.AddMinutes(-30), _crash2, _crash1);

        since.ShouldBe(_crash1.AddTicks(1));
        until.ShouldBe(_crash2.AddMinutes(2));
    }
}

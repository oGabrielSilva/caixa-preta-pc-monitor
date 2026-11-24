using PcMonitor.Domain.Incidents;

namespace PcMonitor.Domain.Tests.Incidents;

public sealed class RelatedEventScopeTests
{
    private static readonly DateTimeOffset _crash1 = new(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _crash2 = _crash1.AddMinutes(10);

    [Theory]
    [InlineData(30, true)]   // boot events right after crash 1's 41: crash 1
    [InlineData(-300, true)] // before crash 1, no previous crash
    [InlineData(121, false)] // after crash 1's boot settled: belongs to the next crash
    public void Events_belong_to_the_first_crash_up_to_two_minutes_after_its_event(int secondsFromCrash1, bool belongs) =>
        RelatedEventScope.Belongs(_crash1.AddSeconds(secondsFromCrash1), _crash1, previousLoggedAt: null).ShouldBe(belongs);

    [Theory]
    [InlineData(30, false)]  // crash 1's boot events never belong to crash 2
    [InlineData(300, true)]
    [InlineData(600 + 120, true)]
    [InlineData(600 + 121, false)]
    public void Events_of_the_second_crash_start_after_the_first_crash_boot_settled(int secondsFromCrash1, bool belongs) =>
        RelatedEventScope.Belongs(_crash1.AddSeconds(secondsFromCrash1), _crash2, previousLoggedAt: _crash1).ShouldBe(belongs);

    [Fact]
    public void Query_window_is_clipped_to_the_scope()
    {
        var (since, until) = RelatedEventScope.Clip(_crash1.AddMinutes(-30), _crash2, _crash1);

        since.ShouldBe(_crash1.AddMinutes(2).AddTicks(1));
        until.ShouldBe(_crash2.AddMinutes(2));
    }
}

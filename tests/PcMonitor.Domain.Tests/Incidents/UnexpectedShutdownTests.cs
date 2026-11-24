using PcMonitor.Domain.Incidents;

namespace PcMonitor.Domain.Tests.Incidents;

public sealed class UnexpectedShutdownTests
{
    private static UnexpectedShutdown Shutdown(long bugcheckCode, int sleepState = 0) => new(
        RecordId: 1, LoggedAt: new DateTimeOffset(2026, 11, 24, 16, 54, 38, TimeSpan.Zero), BugcheckCode: bugcheckCode,
        SleepState: sleepState, PowerButtonPressedAt: null, LongPowerButtonPress: false, LastAliveAt: null,
        Data: new Dictionary<string, string>());

    [Theory]
    [InlineData(0, false)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void SleepInProgress_is_true_for_any_power_transition_state(int sleepState, bool expected) =>
        Shutdown(0, sleepState).SleepInProgress.ShouldBe(expected);

    [Fact]
    public void Shutdown_without_a_bugcheck_code_had_no_bugcheck() => Shutdown(0).HadBugcheck.ShouldBeFalse();

    [Fact]
    public void Shutdown_with_a_bugcheck_code_had_a_bugcheck() => Shutdown(0x9C).HadBugcheck.ShouldBeTrue();

    [Fact]
    public void Bugcheck_codes_above_the_int_range_keep_their_value() =>
        Shutdown(0xC000021A).BugcheckCode.ShouldBe(0xC000021AL);
}

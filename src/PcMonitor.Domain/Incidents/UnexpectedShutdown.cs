namespace PcMonitor.Domain.Incidents;

/// <summary>
/// A Kernel-Power 41 event: Windows found, at boot, that the previous session ended without a clean shutdown.
/// </summary>
/// <param name="RecordId">Event log record id.</param>
/// <param name="LoggedAt">When the event was logged: shortly after the <em>next</em> boot, not at the failure.</param>
/// <param name="BugcheckCode">Blue-screen code, 0 when none was recorded (codes use the full 32-bit unsigned range).</param>
/// <param name="SleepInProgress">Whether the machine was entering or in sleep/hibernate when it went down.</param>
/// <param name="PowerButtonPressedAt">Last power-button press before the failure, if any.</param>
/// <param name="LongPowerButtonPress">Whether a forced power-off (long press) was detected.</param>
/// <param name="LastAliveAt">Last moment Windows recorded itself alive (event 6008), a lower bound for the failure.</param>
/// <param name="Data">All event data, by name.</param>
public sealed record UnexpectedShutdown(
    long RecordId,
    DateTimeOffset LoggedAt,
    long BugcheckCode,
    bool SleepInProgress,
    DateTimeOffset? PowerButtonPressedAt,
    bool LongPowerButtonPress,
    DateTimeOffset? LastAliveAt,
    IReadOnlyDictionary<string, string> Data)
{
    /// <summary>Whether Windows recorded a blue screen for this shutdown.</summary>
    public bool HadBugcheck => BugcheckCode != 0;
}

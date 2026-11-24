namespace PcMonitor.Domain.Incidents;

/// <summary>
/// Which event log entries belong to which incident, so that each entry is attributed to exactly one crash. Used both
/// when incidents are detected and when they are diagnosed.
/// </summary>
/// <remarks>
/// At each boot Windows writes the previous crash's Kernel-Power 41 and 6008 first, then the event log start (6005).
/// So a crash owns: what happened after the previous crash's boot settled, up to two minutes after its own 41; except
/// the 6005, which marks the start of the session the <em>next</em> crash ends.
/// </remarks>
public static class RelatedEventScope
{
    /// <summary>Events this long after a 41 still belong to that crash's boot (6008, boot-time WHEA).</summary>
    public static readonly TimeSpan BootSettle = TimeSpan.FromMinutes(2);

    private const string EventLogProvider = "EventLog";
    private const int EventLogStarted = 6005;
    private static readonly TimeSpan _tick = TimeSpan.FromTicks(1);

    public static bool Belongs(SystemEvent systemEvent, DateTimeOffset loggedAt, DateTimeOffset? previousLoggedAt)
    {
        ArgumentNullException.ThrowIfNull(systemEvent);
        var at = systemEvent.LoggedAt;
        if (systemEvent.Provider == EventLogProvider && systemEvent.EventId == EventLogStarted)
        {
            return at <= loggedAt && (previousLoggedAt is not { } previousBoot || at > previousBoot);
        }

        return at <= loggedAt + BootSettle && (previousLoggedAt is not { } previous || at > previous + BootSettle);
    }

    /// <summary>
    /// A query window that contains every event the incident may own; filter the results with <see cref="Belongs"/>.
    /// </summary>
    public static (DateTimeOffset Since, DateTimeOffset Until) Window(DateTimeOffset since, DateTimeOffset loggedAt, DateTimeOffset? previousLoggedAt)
    {
        var start = previousLoggedAt is { } previous && since <= previous ? previous + _tick : since;
        return (start, loggedAt + BootSettle);
    }
}

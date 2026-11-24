namespace PcMonitor.Domain.Incidents;

/// <summary>
/// Which event log entries belong to which incident, so that each entry is attributed to exactly one crash.
/// A crash owns what happened after the previous crash's boot settled, up to two minutes after its own Kernel-Power 41
/// (the boot right after it). Used both when incidents are detected and when they are diagnosed.
/// </summary>
public static class RelatedEventScope
{
    /// <summary>Events this long after a 41 still belong to that crash's boot (6008, 6005, boot-time WHEA).</summary>
    public static readonly TimeSpan BootSettle = TimeSpan.FromMinutes(2);

    private static readonly TimeSpan _tick = TimeSpan.FromTicks(1);

    public static bool Belongs(DateTimeOffset eventAt, DateTimeOffset loggedAt, DateTimeOffset? previousLoggedAt) =>
        eventAt <= loggedAt + BootSettle && (previousLoggedAt is not { } previous || eventAt > previous + BootSettle);

    /// <summary>Narrows a query window to the incident's scope.</summary>
    public static (DateTimeOffset Since, DateTimeOffset Until) Clip(DateTimeOffset since, DateTimeOffset loggedAt, DateTimeOffset? previousLoggedAt)
    {
        var start = previousLoggedAt is { } previous && since <= previous + BootSettle ? previous + BootSettle + _tick : since;
        return (start, loggedAt + BootSettle);
    }
}

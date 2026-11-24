using PcMonitor.Domain.Incidents;

namespace PcMonitor.Application.Incidents;

/// <summary>
/// Identity of an incident: its Kernel-Power 41 record id and logging time in Unix milliseconds. Milliseconds, not
/// <see cref="DateTimeOffset"/>: the event log carries 100 ns ticks but storage keeps milliseconds, and the two must
/// compare equal.
/// </summary>
public readonly record struct IncidentKey(long RecordId, long LoggedAtUnixMs)
{
    public static IncidentKey Of(UnexpectedShutdown shutdown)
    {
        ArgumentNullException.ThrowIfNull(shutdown);
        return new(shutdown.RecordId, shutdown.LoggedAt.ToUnixTimeMilliseconds());
    }
}

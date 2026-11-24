using PcMonitor.Domain.Incidents;

namespace PcMonitor.Application.Incidents;

/// <summary>Identity of an incident: its Kernel-Power 41 record id and logging time.</summary>
public readonly record struct IncidentKey(long RecordId, DateTimeOffset LoggedAt)
{
    public static IncidentKey Of(UnexpectedShutdown shutdown)
    {
        ArgumentNullException.ThrowIfNull(shutdown);
        return new(shutdown.RecordId, shutdown.LoggedAt);
    }
}

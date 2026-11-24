using PcMonitor.Domain.Incidents;

namespace PcMonitor.Application.Incidents;

/// <summary>Reads the Windows event log.</summary>
public interface ISystemEventLog
{
    /// <summary>Kernel-Power 41 events logged after <paramref name="after"/> (all when <c>null</c>), oldest first.</summary>
    IReadOnlyList<UnexpectedShutdown> ReadUnexpectedShutdowns(DateTimeOffset? after);

    /// <summary>Events relevant to a failure (shutdowns, bugchecks, WHEA, power) logged between <paramref name="since"/> and <paramref name="until"/>.</summary>
    IReadOnlyList<SystemEvent> ReadRelated(DateTimeOffset since, DateTimeOffset until);
}

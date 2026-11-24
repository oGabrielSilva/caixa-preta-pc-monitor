using PcMonitor.Domain.Incidents;

namespace PcMonitor.Application.Incidents;

/// <summary>Reads the Windows event log.</summary>
public interface ISystemEventLog
{
    /// <summary>
    /// Every Kernel-Power 41 event still in the log, in log order (record id). Log order, not timestamps: the clock at
    /// boot comes from the board's RTC, which a faulty board can get wrong.
    /// </summary>
    IReadOnlyList<UnexpectedShutdown> ReadUnexpectedShutdowns();

    /// <summary>
    /// Events relevant to a failure (shutdowns, bugchecks, WHEA, power) logged between <paramref name="since"/> and
    /// <paramref name="until"/>.
    /// </summary>
    IReadOnlyList<SystemEvent> ReadRelated(DateTimeOffset since, DateTimeOffset until);
}

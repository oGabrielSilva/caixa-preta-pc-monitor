using PcMonitor.Domain.Incidents;

namespace PcMonitor.Infrastructure.Windows.EventLog;

/// <summary>
/// Pairs each Kernel-Power 41 with the 6008 written at the same boot. Both are written when the EventLog service starts,
/// the 6008 just before the 41 in record order. Pairing by record order, not by time, keeps a crash during the next
/// boot from taking the previous boot's 6008.
/// </summary>
public static class ShutdownReportPairing
{
    /// <summary>A 6008 further than this from its 41 in time is not trusted (sanity bound; slow boots take minutes).</summary>
    private static readonly TimeSpan _maxDistance = TimeSpan.FromMinutes(15);

    /// <returns>Every shutdown, in record order, with the 6008 of its boot when there is one.</returns>
    public static IReadOnlyList<(SystemEvent Shutdown, SystemEvent? Report)> Pair(
        IReadOnlyList<SystemEvent> kernelPower41s, IReadOnlyList<SystemEvent> reports6008)
    {
        var shutdowns = kernelPower41s.OrderBy(e => e.RecordId).ToList();
        var reports = reports6008.OrderBy(e => e.RecordId).ToList();
        var pairs = new List<(SystemEvent, SystemEvent?)>(shutdowns.Count);

        for (var i = 0; i < shutdowns.Count; i++)
        {
            var shutdown = shutdowns[i];
            var previousRecord = i > 0 ? shutdowns[i - 1].RecordId : long.MinValue;
            var report = reports.LastOrDefault(r =>
                r.RecordId > previousRecord && r.RecordId < shutdown.RecordId
                && (r.LoggedAt - shutdown.LoggedAt).Duration() <= _maxDistance);
            pairs.Add((shutdown, report));
        }

        return pairs;
    }
}

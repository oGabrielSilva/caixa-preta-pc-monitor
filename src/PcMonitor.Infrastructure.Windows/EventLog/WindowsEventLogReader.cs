using System.Diagnostics.Eventing.Reader;
using PcMonitor.Application.Incidents;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Infrastructure.Windows.EventLog;

/// <summary><see cref="ISystemEventLog"/> on the Windows System log (read-only).</summary>
public sealed class WindowsEventLogReader : ISystemEventLog
{
    /// <summary>The 6008 written at the same boot as a 41 is logged within seconds of it.</summary>
    private static readonly TimeSpan _sameBoot = TimeSpan.FromMinutes(2);

    public IReadOnlyList<UnexpectedShutdown> ReadUnexpectedShutdowns(DateTimeOffset? after)
    {
        var shutdowns = Query(EventLogQueries.UnexpectedShutdowns(after));
        if (shutdowns.Count == 0)
        {
            return [];
        }

        var reports = Query(EventLogQueries.ShutdownReports(after - _sameBoot));
        return [.. shutdowns
            .OrderBy(e => e.LoggedAt)
            .Select(e => UnexpectedShutdownMapper.Map(
                e, reports.FirstOrDefault(r => (r.LoggedAt - e.LoggedAt).Duration() <= _sameBoot)))];
    }

    public IReadOnlyList<SystemEvent> ReadRelated(DateTimeOffset since, DateTimeOffset until) =>
        [.. Query(EventLogQueries.Related(since, until)).OrderBy(e => e.LoggedAt)];

    private static List<SystemEvent> Query(string xpath)
    {
        var events = new List<SystemEvent>();
        using var reader = new EventLogReader(new EventLogQuery(EventLogQueries.SystemLog, PathType.LogName, xpath));
        for (var record = reader.ReadEvent(); record is not null; record = reader.ReadEvent())
        {
            using (record)
            {
                events.Add(EventXml.Parse(record.ToXml()));
            }
        }

        return events;
    }
}

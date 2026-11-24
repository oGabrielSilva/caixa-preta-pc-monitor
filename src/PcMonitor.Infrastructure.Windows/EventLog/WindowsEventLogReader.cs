using System.Diagnostics.Eventing.Reader;
using PcMonitor.Application.Incidents;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Infrastructure.Windows.EventLog;

/// <summary><see cref="ISystemEventLog"/> on the Windows System log (read-only).</summary>
public sealed class WindowsEventLogReader : ISystemEventLog
{
    public IReadOnlyList<UnexpectedShutdown> ReadUnexpectedShutdowns() =>
        [.. ShutdownReportPairing.Pair(Query(EventLogQueries.UnexpectedShutdowns), Query(EventLogQueries.ShutdownReports))
            .Select(pair => UnexpectedShutdownMapper.Map(pair.Shutdown, pair.Report))];

    public IReadOnlyList<SystemEvent> ReadRelated(DateTimeOffset since, DateTimeOffset until) =>
        [.. Query(EventLogQueries.Related(since, until)).OrderBy(e => e.LoggedAt)];

    /// <summary>Records that cannot be parsed are skipped: one malformed entry must not hide every other one.</summary>
    private static List<SystemEvent> Query(string xpath)
    {
        var events = new List<SystemEvent>();
        using var reader = new EventLogReader(new EventLogQuery(EventLogQueries.SystemLog, PathType.LogName, xpath));
        for (var record = reader.ReadEvent(); record is not null; record = reader.ReadEvent())
        {
            using (record)
            {
                try
                {
                    events.Add(EventXml.Parse(record.ToXml()));
                }
                catch (Exception exception) when (exception is FormatException or OverflowException or System.Xml.XmlException)
                {
                    // Skip the malformed record.
                }
            }
        }

        return events;
    }
}

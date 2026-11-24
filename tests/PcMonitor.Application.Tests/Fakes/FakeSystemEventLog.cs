using PcMonitor.Application.Incidents;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Application.Tests.Fakes;

/// <summary>Event log with scripted entries; records the windows it was asked for.</summary>
internal sealed class FakeSystemEventLog : ISystemEventLog
{
    public List<UnexpectedShutdown> Shutdowns { get; } = [];

    public List<SystemEvent> Events { get; } = [];

    public List<(DateTimeOffset Since, DateTimeOffset Until)> RelatedQueries { get; } = [];

    public IReadOnlyList<UnexpectedShutdown> ReadUnexpectedShutdowns() => [.. Shutdowns.OrderBy(s => s.RecordId)];

    public IReadOnlyList<SystemEvent> ReadRelated(DateTimeOffset since, DateTimeOffset until)
    {
        RelatedQueries.Add((since, until));
        return [.. Events.Where(e => e.LoggedAt >= since && e.LoggedAt <= until)];
    }
}

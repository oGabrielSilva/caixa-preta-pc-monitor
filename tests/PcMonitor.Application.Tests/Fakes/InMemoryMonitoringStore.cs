using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Application.Tests.Fakes;

/// <summary>Records every call so tests can assert what the recorder persisted.</summary>
internal sealed class InMemoryMonitoringStore : IMonitoringStore
{
    public List<(long Id, DateTimeOffset StartedAt, DateTimeOffset BootedAt, SensorSourceInfo Source)> Sessions { get; } = [];
    public List<(long Id, long SessionId, DateTimeOffset StartedAt, SensorCatalog Catalog)> Segments { get; } = [];
    public List<(long SegmentId, Frame Frame)> Frames { get; } = [];
    public List<(long SegmentId, DateTimeOffset EndedAt, EndReason Reason)> EndedSegments { get; } = [];
    public List<(long SessionId, DateTimeOffset EndedAt, EndReason Reason)> EndedSessions { get; } = [];

    public Exception? AppendFailure { get; set; }

    public long StartSession(DateTimeOffset startedAt, DateTimeOffset bootedAt, SensorSourceInfo source)
    {
        Sessions.Add((Sessions.Count + 1, startedAt, bootedAt, source));
        return Sessions.Count;
    }

    public long StartSegment(long sessionId, DateTimeOffset startedAt, SensorCatalog catalog)
    {
        Segments.Add((Segments.Count + 1, sessionId, startedAt, catalog));
        return Segments.Count;
    }

    public void AppendFrame(long segmentId, Frame frame)
    {
        if (AppendFailure is not null)
        {
            throw AppendFailure;
        }

        Frames.Add((segmentId, frame));
    }

    public void EndSegment(long segmentId, DateTimeOffset endedAt, EndReason reason) =>
        EndedSegments.Add((segmentId, endedAt, reason));

    public void EndSession(long sessionId, DateTimeOffset endedAt, EndReason reason) =>
        EndedSessions.Add((sessionId, endedAt, reason));
}

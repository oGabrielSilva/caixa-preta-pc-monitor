using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Application.Monitoring;

/// <summary>Durable storage for sessions, segments and frames. Every call is committed before it returns.</summary>
public interface IMonitoringStore
{
    long StartSession(DateTimeOffset startedAt, DateTimeOffset bootedAt, SensorSourceInfo source);

    long StartSegment(long sessionId, DateTimeOffset startedAt, SensorCatalog catalog);

    void AppendFrame(long segmentId, Frame frame);

    void EndSegment(long segmentId, DateTimeOffset endedAt, EndReason reason);

    void EndSession(long sessionId, DateTimeOffset endedAt, EndReason reason);
}

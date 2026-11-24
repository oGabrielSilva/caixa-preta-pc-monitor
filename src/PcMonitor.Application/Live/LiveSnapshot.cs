using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Application.Live;

/// <summary>The most recent segment: its sensors and the frames of the requested window, oldest first.</summary>
public sealed record LiveSnapshot(SessionView Session, long SegmentId, SensorCatalog Catalog, IReadOnlyList<Frame> Frames);

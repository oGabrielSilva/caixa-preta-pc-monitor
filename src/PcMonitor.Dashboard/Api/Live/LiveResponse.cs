using PcMonitor.Application.Live;
using PcMonitor.Dashboard.Api.Sensors;

namespace PcMonitor.Dashboard.Api.Live;

/// <summary>What is being recorded now: the latest segment's sensors and its last ten minutes.</summary>
/// <param name="State">Whether frames are arriving.</param>
/// <param name="Session"><c>null</c> when nothing was ever recorded.</param>
/// <param name="SegmentId">Segment to follow on the stream.</param>
/// <param name="Sensors">The sensors, in the order of each frame's values.</param>
/// <param name="Frames">Oldest first.</param>
public sealed record LiveResponse(
    LiveState State,
    SessionResponse? Session,
    long? SegmentId,
    IReadOnlyList<SensorResponse> Sensors,
    IReadOnlyList<FramePoint> Frames);

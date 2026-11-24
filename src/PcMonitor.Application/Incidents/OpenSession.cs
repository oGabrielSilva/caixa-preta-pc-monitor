namespace PcMonitor.Application.Incidents;

/// <summary>A monitoring session that never recorded how it ended.</summary>
/// <param name="SessionId">The session.</param>
/// <param name="StartedAt">When it started.</param>
/// <param name="LastSegmentId">Its last segment, open or closed (e.g. closed by a suspend).</param>
/// <param name="OpenSegmentId">Its segment still open, if any.</param>
/// <param name="LastFrameAt">Time of its last frame, if it recorded any.</param>
public sealed record OpenSession(
    long SessionId, DateTimeOffset StartedAt, long? LastSegmentId, long? OpenSegmentId, DateTimeOffset? LastFrameAt);

namespace PcMonitor.Application.Incidents;

/// <summary>A monitoring session that never recorded how it ended.</summary>
/// <param name="SessionId">The session.</param>
/// <param name="StartedAt">When it started.</param>
/// <param name="BootedAt">When the machine had booted, as measured by that session.</param>
/// <param name="LastSegmentId">Segment holding its last frame (or its last segment when it has no frames).</param>
/// <param name="OpenSegmentId">Its segment still open, if any.</param>
/// <param name="LastFrameAt">Time of its last frame, if it recorded any.</param>
public sealed record OpenSession(
    long SessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset BootedAt,
    long? LastSegmentId,
    long? OpenSegmentId,
    DateTimeOffset? LastFrameAt);

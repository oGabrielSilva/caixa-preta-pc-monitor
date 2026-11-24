namespace PcMonitor.Application.Live;

/// <summary>Where recording stands, without frames: the latest segment and session and the time of the last frame.</summary>
public sealed record LiveHead(long? SegmentId, SessionView? Session, DateTimeOffset? LastFrameAt);

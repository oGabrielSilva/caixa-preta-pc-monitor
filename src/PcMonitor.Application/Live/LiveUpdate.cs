using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Live;

/// <summary>Something a live client needs to know.</summary>
public abstract record LiveUpdate;

/// <summary>New frames of the segment the client follows.</summary>
public sealed record FramesAppended(long SegmentId, IReadOnlyList<Frame> Frames) : LiveUpdate;

/// <summary>Recording moved to another segment (new session, resume, gap): the client reloads its snapshot.</summary>
public sealed record SegmentChanged(long SegmentId) : LiveUpdate;

/// <summary>Periodic report of the recording state, so a client notices when frames stop.</summary>
public sealed record StateReported(LiveState State) : LiveUpdate;

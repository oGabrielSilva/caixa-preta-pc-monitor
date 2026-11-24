using System.Text.Json.Serialization;
using PcMonitor.Application.Live;

namespace PcMonitor.Dashboard.Api.Live;

/// <summary>Kind of a live stream event; also its SSE <c>event</c> name, in lower case.</summary>
public enum LiveEventKind
{
    /// <summary>New frames of the followed segment.</summary>
    Frames,

    /// <summary>Recording moved to another segment: reload <c>/api/live</c>.</summary>
    Segment,

    /// <summary>Periodic recording state (every 5 s), also a heartbeat.</summary>
    State,
}

/// <summary>One server-sent event of <c>/api/live/stream</c>; only the fields of its kind are set.</summary>
public sealed record LiveEvent(LiveEventKind Kind, long? SegmentId, IReadOnlyList<FramePoint>? Frames, LiveState? State)
{
    /// <summary>The SSE <c>event</c> name; not part of the data.</summary>
    [JsonIgnore]
    public string EventType => Kind.ToString().ToLowerInvariant();

    public static LiveEvent From(LiveUpdate update) => update switch
    {
        FramesAppended appended => new(LiveEventKind.Frames, appended.SegmentId, [.. appended.Frames.Select(FramePoint.From)], null),
        SegmentChanged changed => new(LiveEventKind.Segment, changed.SegmentId, null, null),
        StateReported reported => new(LiveEventKind.State, null, null, reported.State),
        _ => throw new ArgumentOutOfRangeException(nameof(update), update, null),
    };
}

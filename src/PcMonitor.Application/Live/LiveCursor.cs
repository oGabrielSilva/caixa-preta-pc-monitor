namespace PcMonitor.Application.Live;

/// <summary>
/// One client's position in the recording. Each <see cref="Poll"/> reports what changed since the previous one.
/// Not thread-safe: one cursor per client.
/// </summary>
public sealed class LiveCursor(ILiveReader reader, TimeProvider time, long? segmentId, DateTimeOffset after)
{
    /// <summary>The state is reported on every n-th poll.</summary>
    public const int StateEveryPolls = 5;

    /// <summary>
    /// The most a client is sent at once: a tab reconnecting hours later gets the last minutes (what its live page
    /// shows), not hours of frames in one event.
    /// </summary>
    public static readonly TimeSpan MaxCatchUp = TimeSpan.FromMinutes(10);

    private long? _segmentId = segmentId;
    private DateTimeOffset _after = after;
    private DateTimeOffset? _lastFrameAt;
    private int _polls;

    /// <summary>Reads the head and the new frames. Throws when the database cannot be read; the next poll retries.</summary>
    public IReadOnlyList<LiveUpdate> Poll()
    {
        _polls++;
        var head = reader.Head();
        var updates = new List<LiveUpdate>();
        _lastFrameAt = head.LastFrameAt;

        if (head.SegmentId is { } current && current != _segmentId)
        {
            // The client reloads its snapshot, which holds everything up to now; follow from the head onwards.
            _segmentId = current;
            _after = head.LastFrameAt ?? DateTimeOffset.MinValue;
            updates.Add(new SegmentChanged(current));
        }
        else if (_segmentId is { } followed)
        {
            var since = head.LastFrameAt is { } last && last - MaxCatchUp > _after ? last - MaxCatchUp : _after;
            var frames = reader.FramesAfter(followed, since);
            if (frames.Count > 0)
            {
                _after = frames[^1].Timestamp;
                updates.Add(new FramesAppended(followed, frames));
            }
        }

        if (_polls % StateEveryPolls == 0)
        {
            updates.Add(new StateReported(LiveStateRule.Of(head.Session, _lastFrameAt, time.GetUtcNow())));
        }

        return updates;
    }
}

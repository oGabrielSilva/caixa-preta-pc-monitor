using PcMonitor.Application.Live;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Tests.Fakes;

/// <summary>Scriptable <see cref="ILiveReader"/>: tests set the head and the frames of each segment.</summary>
internal sealed class FakeLiveReader : ILiveReader
{
    private readonly Dictionary<long, List<Frame>> _frames = [];

    public SessionView? Session { get; set; }

    public long? SegmentId { get; set; }

    public int FailuresLeft { get; set; }

    public int Reads { get; private set; }

    public void Add(long segmentId, params Frame[] frames)
    {
        if (!_frames.TryGetValue(segmentId, out var list))
        {
            _frames[segmentId] = list = [];
        }

        list.AddRange(frames);
    }

    public LiveHead Head()
    {
        Reads++;
        if (FailuresLeft > 0)
        {
            FailuresLeft--;
            throw new InvalidOperationException("database is locked");
        }

        var last = SegmentId is { } id && _frames.TryGetValue(id, out var list) && list.Count > 0 ? list[^1].Timestamp : (DateTimeOffset?)null;
        return new LiveHead(SegmentId, Session, last);
    }

    public IReadOnlyList<Frame> FramesAfter(long segmentId, DateTimeOffset after) =>
        _frames.TryGetValue(segmentId, out var list) ? [.. list.Where(f => f.Timestamp > after)] : [];

    public LiveSnapshot? Latest(TimeSpan window) => throw new NotSupportedException();
}

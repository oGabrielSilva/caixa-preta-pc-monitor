using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Live;

/// <summary>Reads what the collector is recording now.</summary>
public interface ILiveReader
{
    /// <summary>The latest segment of the latest session with the frames of the last <paramref name="window"/>; <c>null</c> without sessions.</summary>
    LiveSnapshot? Latest(TimeSpan window);

    /// <summary>Frames of <paramref name="segmentId"/> newer than <paramref name="after"/>, oldest first.</summary>
    IReadOnlyList<Frame> FramesAfter(long segmentId, DateTimeOffset after);

    /// <summary>The latest segment and session, cheap enough to call every second.</summary>
    LiveHead Head();
}

using Microsoft.Extensions.Logging;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Monitoring;

/// <summary>
/// Owns the state of one monitoring session: opens the sensor source, records one frame per call and splits the
/// session into segments on suspend, explicit breaks and gaps. Not thread-safe: use it from a single thread.
/// </summary>
public sealed partial class MonitoringRecorder(
    ISensorSource source,
    IMonitoringStore store,
    TimeProvider time,
    ILogger<MonitoringRecorder> logger,
    TimeSpan gapThreshold,
    Func<TimeSpan> uptime)
{
    private SensorSourceInfo? _source;
    private long _sessionId;
    private long? _segmentId;
    private DateTimeOffset _lastFrameAt;

    /// <summary>Opens the sensor source and starts a session with its first segment.</summary>
    public SensorSourceInfo Start()
    {
        _source = source.Open();
        var now = time.GetUtcNow();
        _sessionId = store.StartSession(now, now - uptime(), _source);
        OpenSegment(now);
        LogSessionStarted(logger, _sessionId, _source.Status, _source.Catalog.Count, _source.BoardProduct);
        return _source;
    }

    /// <summary>
    /// Reads the sensors and appends one frame. Returns <c>false</c> when reading or writing failed;
    /// the error is logged and the next call tries again.
    /// </summary>
    public bool RecordOnce()
    {
        EnsureStarted();
        var now = time.GetUtcNow();

        try
        {
            if (_segmentId is { } current && now - _lastFrameAt > gapThreshold)
            {
                CloseSegment(current, _lastFrameAt, EndReason.Gap);
            }

            var values = source.Read();
            var segmentId = _segmentId ?? OpenSegment(now);
            store.AppendFrame(segmentId, new Frame(now, values));
            _lastFrameAt = now;
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogRecordFailed(logger, exception);
            return false;
        }
    }

    /// <summary>Ends the current segment; the next <see cref="RecordOnce"/> starts a new one.</summary>
    public void BreakSegment(EndReason reason)
    {
        EnsureStarted();
        if (_segmentId is { } current)
        {
            CloseSegment(current, time.GetUtcNow(), reason);
        }
    }

    /// <summary>Ends the current segment (if any) and the session.</summary>
    public void Stop(EndReason reason)
    {
        EnsureStarted();
        var now = time.GetUtcNow();
        if (_segmentId is { } current)
        {
            CloseSegment(current, now, reason);
        }

        store.EndSession(_sessionId, now, reason);
        LogSessionEnded(logger, _sessionId, reason);
    }

    private long OpenSegment(DateTimeOffset at)
    {
        var segmentId = store.StartSegment(_sessionId, at, _source!.Catalog);
        _segmentId = segmentId;
        _lastFrameAt = at;
        return segmentId;
    }

    private void CloseSegment(long segmentId, DateTimeOffset at, EndReason reason)
    {
        _segmentId = null;
        store.EndSegment(segmentId, at, reason);
        LogSegmentEnded(logger, segmentId, reason);
    }

    private void EnsureStarted()
    {
        if (_source is null)
        {
            throw new InvalidOperationException($"{nameof(Start)} must be called first.");
        }
    }

    [LoggerMessage(EventId = 100, Level = LogLevel.Information,
        Message = "Monitoring session {SessionId} started: sensors {Status}, {SensorCount} sensors, board '{Board}'.")]
    private static partial void LogSessionStarted(
        ILogger logger, long sessionId, SensorSourceStatus status, int sensorCount, string? board);

    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Segment {SegmentId} ended: {Reason}.")]
    private static partial void LogSegmentEnded(ILogger logger, long segmentId, EndReason reason);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "Monitoring session {SessionId} ended: {Reason}.")]
    private static partial void LogSessionEnded(ILogger logger, long sessionId, EndReason reason);

    [LoggerMessage(EventId = 103, Level = LogLevel.Error, Message = "Recording a frame failed; retrying on the next cycle.")]
    private static partial void LogRecordFailed(ILogger logger, Exception exception);
}

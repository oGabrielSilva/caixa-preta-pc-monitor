using Microsoft.Extensions.Logging;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Monitoring;

/// <summary>
/// Owns the state of one monitoring session: opens the sensor source, records one frame per call and splits the
/// session into segments on suspend and on gaps. Storage failures are logged and retried, never thrown.
/// Not thread-safe: use it from a single thread.
/// </summary>
public sealed partial class MonitoringRecorder(
    ISensorSource source,
    IMonitoringStore store,
    TimeProvider time,
    ILogger<MonitoringRecorder> logger,
    TimeSpan gapThreshold,
    Func<TimeSpan> uptime)
{
    /// <summary>
    /// Cycles to stay paused after a suspend without a resume signal before recording again on its own
    /// (a lost resume notification must not stop recording for good).
    /// </summary>
    public const int MaxCyclesSuspended = 30;

    private SensorSourceInfo? _source;
    private long _sessionId;
    private long? _segmentId;
    private DateTimeOffset _lastFrameAt;
    private bool _suspended;
    private int _cyclesSuspended;

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
    /// Reads the sensors and appends one frame. Returns <c>false</c> when paused by a suspend, or when reading or
    /// writing failed (logged; the next call tries again).
    /// </summary>
    public bool RecordOnce()
    {
        EnsureStarted();
        if (_suspended && !ResumeAfterTooLong())
        {
            return false;
        }

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

    /// <summary>
    /// The machine is going to sleep: ends the current segment at <paramref name="signalledAt"/> (when Windows
    /// announced it) and pauses recording until <see cref="Resume"/>. Returns <c>false</c> if the segment could not be
    /// closed; it is then closed as a gap after resume.
    /// </summary>
    public bool Suspend(DateTimeOffset signalledAt)
    {
        EnsureStarted();
        _suspended = true;
        _cyclesSuspended = 0;
        return _segmentId is not { } current || TryCloseSegment(current, signalledAt, EndReason.Suspended);
    }

    /// <summary>The machine woke up: the next frame starts a new segment.</summary>
    public void Resume()
    {
        EnsureStarted();
        _suspended = false;
    }

    /// <summary>Ends the current segment (if any) and the session. Failures are logged, never thrown.</summary>
    public void Stop(EndReason reason)
    {
        EnsureStarted();
        var now = time.GetUtcNow();
        if (_segmentId is { } current)
        {
            TryCloseSegment(current, now, reason);
        }

        try
        {
            store.EndSession(_sessionId, now, reason);
            LogSessionEnded(logger, _sessionId, reason);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogStopFailed(logger, exception);
        }
    }

    private bool ResumeAfterTooLong()
    {
        if (++_cyclesSuspended <= MaxCyclesSuspended)
        {
            return false;
        }

        LogResumedWithoutSignal(logger, MaxCyclesSuspended);
        _suspended = false;
        return true;
    }

    private long OpenSegment(DateTimeOffset at)
    {
        var segmentId = store.StartSegment(_sessionId, at, _source!.Catalog);
        _segmentId = segmentId;
        _lastFrameAt = at;
        return segmentId;
    }

    /// <summary>Closes the segment in storage first; the in-memory state changes only after it succeeded.</summary>
    private void CloseSegment(long segmentId, DateTimeOffset at, EndReason reason)
    {
        store.EndSegment(segmentId, at, reason);
        _segmentId = null;
        LogSegmentEnded(logger, segmentId, reason);
    }

    private bool TryCloseSegment(long segmentId, DateTimeOffset at, EndReason reason)
    {
        try
        {
            CloseSegment(segmentId, at, reason);
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogCloseFailed(logger, exception, segmentId, reason);
            return false;
        }
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

    [LoggerMessage(EventId = 104, Level = LogLevel.Error, Message = "Ending segment {SegmentId} ({Reason}) failed.")]
    private static partial void LogCloseFailed(ILogger logger, Exception exception, long segmentId, EndReason reason);

    [LoggerMessage(EventId = 105, Level = LogLevel.Error, Message = "Ending the monitoring session failed.")]
    private static partial void LogStopFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 106, Level = LogLevel.Warning,
        Message = "No resume signal after {Cycles} cycles in suspend; recording again.")]
    private static partial void LogResumedWithoutSignal(ILogger logger, int cycles);
}

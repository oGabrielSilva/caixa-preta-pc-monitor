using Microsoft.Extensions.Time.Testing;
using PcMonitor.Application.Tests.Fakes;

namespace PcMonitor.Application.Tests.Live;

/// <summary>The clock and reader shared by the live tests: segment 7 of an open session, at 18:00 UTC.</summary>
public abstract class LiveTestClock
{
    protected static readonly DateTimeOffset T0 = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    private protected FakeTimeProvider Time { get; } = new(T0);

    private protected FakeLiveReader Reader { get; } = new() { Session = LiveStateRuleTests.Session(), SegmentId = 7 };
}

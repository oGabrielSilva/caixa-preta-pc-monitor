using PcMonitor.Application.Live;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Tests.Live;

public sealed class LiveStateRuleTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    internal static SessionView Session(DateTimeOffset? endedAt = null) =>
        new(1, _now.AddHours(-1), SensorSourceStatus.Ok, "TUF GAMING A620M-PLUS WIFI", endedAt, endedAt is null ? null : EndReason.ServiceStopped);

    [Fact]
    public void No_session_is_no_data() => LiveStateRule.Of(null, null, _now).ShouldBe(LiveState.NoData);

    [Fact]
    public void Open_session_with_a_frame_5s_old_is_recording() =>
        LiveStateRule.Of(Session(), _now.AddSeconds(-5), _now).ShouldBe(LiveState.Recording);

    [Fact]
    public void Open_session_with_a_frame_6s_old_is_stale() =>
        LiveStateRule.Of(Session(), _now.AddSeconds(-6), _now).ShouldBe(LiveState.Stale);

    [Fact]
    public void Open_session_without_frames_is_stale() =>
        LiveStateRule.Of(Session(), null, _now).ShouldBe(LiveState.Stale);

    [Fact]
    public void Ended_session_is_stopped() =>
        LiveStateRule.Of(Session(endedAt: _now.AddSeconds(-1)), _now.AddSeconds(-1), _now).ShouldBe(LiveState.Stopped);
}

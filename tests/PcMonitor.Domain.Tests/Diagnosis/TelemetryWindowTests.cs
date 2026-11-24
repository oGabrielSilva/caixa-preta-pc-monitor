using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Domain.Tests.Diagnosis;

public sealed class TelemetryWindowTests
{
    private static readonly DateTimeOffset _end = new(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Telemetry_window_drops_NaN_and_returns_the_tail()
    {
        var window = new TelemetryWindow(_end, new Dictionary<SensorRole, IReadOnlyList<(DateTimeOffset, float)>>
        {
            [SensorRole.CpuLoad] = [(_end.AddSeconds(-20), 10f), (_end.AddSeconds(-9), float.NaN), (_end.AddSeconds(-5), 50f), (_end, 70f)],
        });

        window.Series(SensorRole.CpuLoad).Count.ShouldBe(3);
        window.Tail(SensorRole.CpuLoad, TimeSpan.FromSeconds(10)).Select(p => p.Value).ShouldBe([50f, 70f]);
        window.Has(SensorRole.CpuLoad).ShouldBeTrue();
    }

    [Fact]
    public void Missing_role_has_an_empty_series()
    {
        var window = new TelemetryWindow(_end, new Dictionary<SensorRole, IReadOnlyList<(DateTimeOffset, float)>>());

        window.Has(SensorRole.Rail12V).ShouldBeFalse();
        window.Series(SensorRole.Rail12V).ShouldBeEmpty();
    }
}

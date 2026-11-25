using Microsoft.Extensions.Time.Testing;
using PcMonitor.Application.Retention;

namespace PcMonitor.Application.Tests.Retention;

public sealed class ApplyRetentionTests
{
    private sealed class RecordingRetentionStore : IRetentionStore
    {
        public List<string> Calls { get; } = [];

        public int AggregateMinutesBefore(DateTimeOffset cutoff)
        {
            Calls.Add($"minutes<{cutoff:O}");
            return 3;
        }

        public int AggregateHoursBefore(DateTimeOffset cutoff)
        {
            Calls.Add($"hours<{cutoff:O}");
            return 2;
        }

        public int DeleteFramesBefore(DateTimeOffset cutoff, TimeSpan incidentWindow)
        {
            Calls.Add($"frames<{cutoff:O} keep {incidentWindow.TotalSeconds}s");
            return 120;
        }

        public int DeleteMinuteAggregatesBefore(DateTimeOffset cutoff)
        {
            Calls.Add($"minute-aggregates<{cutoff:O}");
            return 60;
        }
    }

    [Fact]
    public void Retention_summarises_minutes_and_hours_then_deletes_old_frames_and_old_minute_aggregates()
    {
        var now = new DateTimeOffset(2026, 11, 24, 12, 34, 56, TimeSpan.Zero);
        var store = new RecordingRetentionStore();
        var policy = new RetentionPolicy(TimeSpan.FromDays(14), TimeSpan.FromDays(90), TimeSpan.FromSeconds(120));

        var result = new ApplyRetention(store, policy, new FakeTimeProvider(now)).Run();

        store.Calls.ShouldBe(
        [
            "minutes<2026-11-24T12:34:00.0000000+00:00",
            "hours<2026-11-24T12:00:00.0000000+00:00",
            "frames<2026-11-10T12:34:56.0000000+00:00 keep 120s",
            "minute-aggregates<2026-08-26T12:34:56.0000000+00:00",
        ]);
        result.ShouldBe(new RetentionResult(MinutesAggregated: 3, HoursAggregated: 2, FramesDeleted: 120, MinuteAggregatesDeleted: 60));
    }
}

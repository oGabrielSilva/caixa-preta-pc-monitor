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
            Calls.Add($"aggregate<{cutoff:O}");
            return 3;
        }

        public int DeleteFramesBefore(DateTimeOffset cutoff, TimeSpan incidentWindow)
        {
            Calls.Add($"delete<{cutoff:O} keep {incidentWindow.TotalSeconds}s");
            return 120;
        }
    }

    [Fact]
    public void Retention_aggregates_complete_minutes_then_deletes_frames_older_than_14_days()
    {
        var now = new DateTimeOffset(2026, 11, 24, 12, 34, 56, TimeSpan.Zero);
        var store = new RecordingRetentionStore();

        var result = new ApplyRetention(store, new RetentionPolicy(TimeSpan.FromDays(14), TimeSpan.FromSeconds(120)), new FakeTimeProvider(now)).Run();

        store.Calls.ShouldBe(
        [
            "aggregate<2026-11-24T12:34:00.0000000+00:00",
            "delete<2026-11-10T12:34:56.0000000+00:00 keep 120s",
        ]);
        result.ShouldBe(new RetentionResult(MinutesAggregated: 3, FramesDeleted: 120));
    }
}

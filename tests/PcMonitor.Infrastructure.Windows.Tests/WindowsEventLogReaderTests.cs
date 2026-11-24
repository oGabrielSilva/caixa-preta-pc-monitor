using PcMonitor.Infrastructure.Windows.EventLog;

namespace PcMonitor.Infrastructure.Windows.Tests;

public sealed class WindowsEventLogReaderTests
{
    [Fact]
    public void Reader_reads_this_machines_system_log_without_throwing()
    {
        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var reader = new WindowsEventLogReader();

        var shutdowns = reader.ReadUnexpectedShutdowns(after: null);
        var related = reader.ReadRelated(since, DateTimeOffset.UtcNow);

        shutdowns.ShouldBe(shutdowns.OrderBy(s => s.LoggedAt).ToList());
        related.ShouldAllBe(e => e.LoggedAt >= since);
    }

    [Fact]
    public void Reader_returns_only_shutdowns_after_the_given_time()
    {
        var reader = new WindowsEventLogReader();
        var all = reader.ReadUnexpectedShutdowns(after: null);
        if (all.Count == 0)
        {
            return; // Machine without crashes (e.g. CI): nothing to filter.
        }

        reader.ReadUnexpectedShutdowns(after: all[^1].LoggedAt).ShouldBeEmpty();
    }
}

using PcMonitor.Infrastructure.Windows.EventLog;

namespace PcMonitor.Infrastructure.Windows.Tests;

public sealed class WindowsEventLogReaderTests
{
    [Fact]
    public void Reader_reads_this_machines_system_log_in_record_order()
    {
        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var reader = new WindowsEventLogReader();

        var shutdowns = reader.ReadUnexpectedShutdowns();
        var related = reader.ReadRelated(since, DateTimeOffset.UtcNow);

        shutdowns.Select(s => s.RecordId).ShouldBe(shutdowns.Select(s => s.RecordId).Order());
        related.ShouldAllBe(e => e.LoggedAt >= since);
    }
}

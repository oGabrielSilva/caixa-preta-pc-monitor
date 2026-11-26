namespace PcMonitor.Tray.Tests;

public sealed class DependencyTests
{
    /// <summary>The tray app only talks HTTP to the dashboard: it references no PC Monitor project.</summary>
    [Fact]
    public void Tray_references_no_pc_monitor_project() =>
        typeof(PcMonitor.Tray.Startup.DashboardAddress).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name).ShouldNotContain(name => name!.StartsWith("PcMonitor.", StringComparison.Ordinal));
}

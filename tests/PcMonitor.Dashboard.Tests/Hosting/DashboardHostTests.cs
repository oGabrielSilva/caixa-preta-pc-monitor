using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using PcMonitor.Dashboard.Configuration;
using PcMonitor.Dashboard.Tests.Support;
using PcMonitor.Infrastructure.Persistence.Sqlite;

namespace PcMonitor.Dashboard.Tests.Hosting;

public sealed class DashboardHostTests
{
    [Fact]
    public void Event_log_is_not_used_outside_a_windows_service()
    {
        using var factory = DashboardFactory.WithoutDatabase();

        factory.Services.GetServices<ILoggerProvider>().ShouldNotContain(p => p is EventLogLoggerProvider);
    }

    [Fact]
    public void Database_path_defaults_to_the_collector_path() =>
        new DashboardOptions().DatabasePath.ShouldBe(SqliteDatabase.DefaultPath);

    [Fact]
    public void Database_comes_from_the_configured_path()
    {
        using var factory = DashboardFactory.WithoutDatabase();

        factory.Services.GetRequiredService<SqliteDatabase>().Path.ShouldStartWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar));
    }
}

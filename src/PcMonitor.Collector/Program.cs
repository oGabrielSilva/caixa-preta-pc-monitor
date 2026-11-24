using Microsoft.Extensions.Hosting.WindowsServices;
using PcMonitor.Collector.Hosting;
using PcMonitor.Infrastructure.Persistence.Sqlite;

var builder = Host.CreateApplicationBuilder(args);

// Only the installed service writes to the Windows Event Log; local runs log to the console and never mix their
// entries with the service's (the owner reads that log to diagnose crashes).
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Logging.AddEventLog(settings => settings.SourceName = CollectorServiceCollectionExtensions.ServiceName);
}
builder.Services.AddCollector();

using var host = builder.Build();
host.Services.GetRequiredService<SchemaMigrator>().Migrate();
host.Services.GetRequiredService<StartupAnalysis>().Run();
await host.RunAsync().ConfigureAwait(false);

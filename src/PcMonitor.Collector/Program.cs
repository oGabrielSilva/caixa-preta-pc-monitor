using PcMonitor.Collector.Hosting;
using PcMonitor.Infrastructure.Persistence.Sqlite;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddEventLog(settings => settings.SourceName = CollectorServiceCollectionExtensions.ServiceName);
builder.Services.AddCollector();

using var host = builder.Build();
host.Services.GetRequiredService<SchemaMigrator>().Migrate();
await host.RunAsync().ConfigureAwait(false);

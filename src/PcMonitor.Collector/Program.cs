var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options => options.ServiceName = "PcMonitor.Collector");
builder.Logging.AddEventLog(settings => settings.SourceName = "PcMonitor.Collector");

builder.Build().Run();

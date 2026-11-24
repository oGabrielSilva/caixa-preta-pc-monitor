using Microsoft.Extensions.Hosting.WindowsServices;
using PcMonitor.Dashboard.Api;
using PcMonitor.Dashboard.Configuration;

// As a service the working directory is System32: the content root (and so wwwroot) must be the install folder.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : null,
});

builder.AddLoopbackDashboard();
builder.AddDashboard();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapApi();
app.MapFallbackToFile("index.html");

app.Run();

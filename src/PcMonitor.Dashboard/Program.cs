using PcMonitor.Dashboard.Api;
using PcMonitor.Dashboard.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddLoopbackDashboard();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapApi();
app.MapFallbackToFile("index.html");

app.Run();

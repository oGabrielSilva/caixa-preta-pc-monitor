using PcMonitor.Dashboard.Api;
using PcMonitor.Dashboard.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddLoopbackDashboard();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapApi();

app.Run();

using System.Runtime.Versioning;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;
using PcMonitor.Application.History;
using PcMonitor.Application.Incidents;
using PcMonitor.Application.Live;
using PcMonitor.Application.Storage;
using PcMonitor.Dashboard.Api.OpenApi;
using PcMonitor.Infrastructure.Persistence.Sqlite;
using PcMonitor.Infrastructure.Persistence.Sqlite.History;
using PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;
using PcMonitor.Infrastructure.Persistence.Sqlite.Live;
using PcMonitor.Infrastructure.Persistence.Sqlite.Storage;

namespace PcMonitor.Dashboard.Configuration;

internal static class DashboardServiceCollectionExtensions
{
    public const string ServiceName = "PcMonitor.Dashboard";

    /// <summary>
    /// Composes the dashboard: Windows service lifetime, the read-only SQLite readers behind the Application ports,
    /// the live feed and history query, JSON with enums as names, and ProblemDetails.
    /// </summary>
    public static WebApplicationBuilder AddDashboard(this WebApplicationBuilder builder)
    {
        ConfigureLogging(builder.Logging);

        var services = builder.Services;
        services.AddWindowsService(options => options.ServiceName = ServiceName);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(provider => new SqliteDatabase(provider.GetRequiredService<IOptions<DashboardOptions>>().Value.DatabasePath));
        services.AddSingleton<IStorageStatus, SqliteStorageStatus>();
        services.AddSingleton<ILiveReader, SqliteLiveReader>();
        services.AddSingleton<IIncidentQuery, SqliteIncidentQuery>();
        services.AddSingleton<IHistoryReader, SqliteHistoryReader>();
        services.AddSingleton<LiveFeed>();
        services.AddSingleton<QueryHistory>();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            // Numbers are numbers: the web default also accepts strings, which makes every number "number | string" in TypeScript.
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });
        services.AddProblemDetails();
        services.AddOpenApi(options => options.AddSchemaTransformer<FindingCodeSchemaTransformer>());
        return builder;
    }

    /// <summary>
    /// Only the installed service writes to the Windows Event Log (source registered by the installer); local runs and
    /// tests log to the console and never mix their entries with the service's.
    /// </summary>
    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
        {
            AddServiceEventLog(logging);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void AddServiceEventLog(ILoggingBuilder logging) =>
        logging.AddEventLog(settings => settings.SourceName = ServiceName);
}

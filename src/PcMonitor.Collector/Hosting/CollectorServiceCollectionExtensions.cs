using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;
using PcMonitor.Application.Monitoring;
using PcMonitor.Application.Retention;
using PcMonitor.Collector.Configuration;
using PcMonitor.Collector.Retention;
using PcMonitor.Collector.Sampling;
using PcMonitor.Infrastructure.Persistence.Sqlite;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;
using PcMonitor.Infrastructure.Persistence.Sqlite.Retention;
using PcMonitor.Infrastructure.Sensors.Lhm;

namespace PcMonitor.Collector.Hosting;

/// <summary>Composition root of the collector.</summary>
internal static class CollectorServiceCollectionExtensions
{
    public const string ServiceName = "PcMonitor.Collector";

    public static IServiceCollection AddCollector(this IServiceCollection services)
    {
        services.AddOptions<CollectorOptions>()
            .BindConfiguration(CollectorOptions.SectionName)
            .PostConfigure(o => o.DatabasePath = Environment.ExpandEnvironmentVariables(o.DatabasePath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddWindowsService(o => o.ServiceName = ServiceName);
        services.AddSingleton<ServiceSignals>();
        if (WindowsServiceHelpers.IsWindowsService())
        {
            services.AddSingleton<IHostLifetime, PcMonitorServiceLifetime>();
        }

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(sp => new SqliteDatabase(Options(sp).DatabasePath));
        services.AddSingleton<SchemaMigrator>();
        services.AddSingleton<IMonitoringStore, SqliteMonitoringStore>();
        services.AddSingleton<IRetentionStore, SqliteRetentionStore>();
        services.AddSingleton<ISensorSource, LhmSensorSource>();

        services.AddSingleton(sp => new MonitoringRecorder(
            sp.GetRequiredService<ISensorSource>(),
            sp.GetRequiredService<IMonitoringStore>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<MonitoringRecorder>>(),
            Options(sp).GapThreshold,
            static () => TimeSpan.FromMilliseconds(Environment.TickCount64)));
        services.AddSingleton(sp => new RetentionPolicy(Options(sp).RawRetention));
        services.AddSingleton<ApplyRetention>();

        services.AddSingleton<IProcessTerminator, FailFastTerminator>();
        services.AddSingleton(sp => new StallWatchdog(
            ChecksIn(Options(sp).StallTimeout),
            ChecksIn(Options(sp).StartupTimeout),
            sp.GetRequiredService<IProcessTerminator>(),
            sp.GetRequiredService<ILogger<StallWatchdog>>()));
        services.AddSingleton<SamplerThread>();

        // The host stops hosted services in reverse order: sampling, registered last, stops first and records the
        // session's end reason before a possibly long retention run is waited for.
        services.AddHostedService<RetentionWorker>();
        services.AddHostedService<SamplingWorker>();

        return services;
    }

    private static int ChecksIn(TimeSpan timeout) => (int)Math.Ceiling(timeout / SamplingWorker.CheckInterval);

    private static CollectorOptions Options(IServiceProvider services) =>
        services.GetRequiredService<IOptions<CollectorOptions>>().Value;
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PcMonitor.Tests.Support;

namespace PcMonitor.Dashboard.Tests.Support;

/// <summary>
/// The real dashboard over a test database and a temporary web root holding a marker <c>index.html</c>.
/// The optional services callback replaces registrations (for example a fake clock) before the host starts.
/// </summary>
public sealed class DashboardFactory : WebApplicationFactory<Program>
{
    public const string SpaMarker = "<title>spa</title>";

    private readonly string _databasePath;
    private readonly string _webRoot = Directory.CreateTempSubdirectory("pcmonitor-webroot-").FullName;
    private readonly Action<IServiceCollection>? _services;

    private DashboardFactory(string databasePath, Action<IServiceCollection>? services)
    {
        _databasePath = databasePath;
        _services = services;
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), $"<!doctype html>{SpaMarker}");
    }

    public static DashboardFactory WithDatabase(TempDatabase database, Action<IServiceCollection>? services = null) =>
        new(database.Path, services);

    /// <summary>A database path that does not exist, as before the collector's first start.</summary>
    public static DashboardFactory WithoutDatabase() =>
        new(Path.Combine(Path.GetTempPath(), $"pcmonitor-missing-{Guid.NewGuid():N}", "pcmonitor.db"), null);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dashboard:DatabasePath", _databasePath);
        builder.UseWebRoot(_webRoot);
        if (_services is not null)
        {
            builder.ConfigureTestServices(_services);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }
}

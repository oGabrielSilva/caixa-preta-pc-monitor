using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PcMonitor.Tests.Support;

namespace PcMonitor.Dashboard.Tests.Support;

/// <summary>A migrated test database, a seed to fill it and the dashboard reading it, disposed together.</summary>
public sealed class SeededDashboard : IDisposable
{
    public static readonly DateTimeOffset Now = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _database = TempDatabase.Migrated();
    private readonly Lazy<DashboardFactory> _factory;

    public SeededDashboard(Action<IServiceCollection>? services = null)
    {
        Seed = new DatabaseSeed(_database.Database);
        _factory = new(() => DashboardFactory.WithDatabase(_database, collection =>
        {
            collection.AddSingleton<TimeProvider>(Time);
            services?.Invoke(collection);
        }));
    }

    public DatabaseSeed Seed { get; }

    public FakeTimeProvider Time { get; } = new(Now);

    public TempDatabase Database => _database;

    public HttpClient Client() => _factory.Value.CreateClient();

    /// <summary>GETs <paramref name="path"/> and parses the JSON body; fails on a non-success status.</summary>
    public async Task<JsonElement> Get(string path)
    {
        using var response = await Client().GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return document.RootElement.Clone();
    }

    public void Dispose()
    {
        if (_factory.IsValueCreated)
        {
            _factory.Value.Dispose();
        }

        Seed.Dispose();
        _database.Dispose();
    }
}

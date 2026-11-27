using System.Net.Http.Json;
using PcMonitor.Application.Storage;
using PcMonitor.Dashboard.Api.Health;
using PcMonitor.Dashboard.Tests.Support;
using PcMonitor.Tests.Support;

namespace PcMonitor.Dashboard.Tests.Api;

public sealed class HealthEndpointsTests
{
    private static async Task<HealthResponse> HealthOf(DashboardFactory factory) =>
        (await factory.CreateClient().GetFromJsonAsync<HealthResponse>("/api/health", ApiJson.Options, TestContext.Current.CancellationToken))!;

    [Fact]
    public async Task Health_returns_ok_with_version_when_there_is_no_database()
    {
        using var factory = DashboardFactory.WithoutDatabase();

        var health = await HealthOf(factory);

        health.ShouldBe(new HealthResponse("ok", "1.0.2", StorageState.Missing));
    }

    [Fact]
    public async Task Health_reports_a_ready_database()
    {
        using var database = TempDatabase.Migrated();
        using var factory = DashboardFactory.WithDatabase(database);

        (await HealthOf(factory)).Storage.ShouldBe(StorageState.Ready);
    }
}

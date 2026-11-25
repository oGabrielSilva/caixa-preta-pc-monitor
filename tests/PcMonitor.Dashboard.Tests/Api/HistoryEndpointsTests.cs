using System.Net;
using PcMonitor.Dashboard.Tests.Support;
using PcMonitor.Infrastructure.Persistence.Sqlite.Retention;

namespace PcMonitor.Dashboard.Tests.Api;

public sealed class HistoryEndpointsTests : IDisposable
{
    private const string Rail12V = "/lpc/nct6799d/0/voltage/4";

    private readonly SeededDashboard _dashboard = new();

    public void Dispose() => _dashboard.Dispose();

    private async Task<HttpStatusCode> StatusOf(string path)
    {
        using var response = await _dashboard.Client().GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    [Fact]
    public async Task Sensors_lists_recorded_sensors_with_ranges()
    {
        _dashboard.Seed.Recording(SeededDashboard.Now.AddHours(-1), 5);

        var sensors = await _dashboard.Get("/api/history/sensors");

        sensors.GetArrayLength().ShouldBe(7);
        var rail = sensors.EnumerateArray().Single(s => s.GetProperty("key").GetString() == Rail12V);
        rail.GetProperty("role").GetString().ShouldBe("Rail12V");
        rail.GetProperty("range").GetProperty("max").GetSingle().ShouldBe(12.6f);
    }

    [Fact]
    public async Task History_returns_day_buckets_of_fifteen_minutes()
    {
        // 30 minutes ending at 17:30: the first 20 aggregated by retention, the last 10 still raw frames.
        var start = SeededDashboard.Now.AddMinutes(-60);
        _dashboard.Seed.Recording(start, 30 * 60);
        new SqliteRetentionStore(_dashboard.Database.Database).AggregateMinutesBefore(start.AddMinutes(20));

        var history = await _dashboard.Get($"/api/history?sensor={Uri.EscapeDataString(Rail12V)}&range=Day");

        history.GetProperty("bucketSeconds").GetInt32().ShouldBe(900);
        history.GetProperty("sensor").GetProperty("key").GetString().ShouldBe(Rail12V);
        history.GetProperty("buckets").EnumerateArray().Select(b => b.GetProperty("t").GetInt64()).ShouldBe(
            [start.ToUnixTimeMilliseconds(), start.AddMinutes(15).ToUnixTimeMilliseconds()]);
        history.GetProperty("buckets")[1].GetProperty("avg").GetDouble().ShouldBe(12.19, tolerance: 0.001);
    }

    [Fact]
    public async Task Unknown_sensor_is_404() =>
        (await StatusOf("/api/history?sensor=%2Funknown&range=Day")).ShouldBe(HttpStatusCode.NotFound);

    [Fact]
    public async Task Invalid_range_is_400() =>
        (await StatusOf($"/api/history?sensor={Uri.EscapeDataString(Rail12V)}&range=Year")).ShouldBe(HttpStatusCode.BadRequest);

    [Fact]
    public async Task Numeric_range_is_400() =>
        (await StatusOf($"/api/history?sensor={Uri.EscapeDataString(Rail12V)}&range=7")).ShouldBe(HttpStatusCode.BadRequest);

    [Fact]
    public async Task History_is_503_without_a_database()
    {
        using var factory = DashboardFactory.WithoutDatabase();

        using var response = await factory.CreateClient().GetAsync(new Uri("/api/history/sensors", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}

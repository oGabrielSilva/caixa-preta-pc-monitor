using System.Net;
using PcMonitor.Dashboard.Tests.Support;

namespace PcMonitor.Dashboard.Tests;

public sealed class SpaHostingTests : IDisposable
{
    private readonly DashboardFactory _factory = DashboardFactory.WithoutDatabase();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Unknown_api_route_returns_404_not_the_spa()
    {
        var response = await _factory.CreateClient().GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain(DashboardFactory.SpaMarker);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/incidents")]
    [InlineData("/incidents/3")]
    [InlineData("/history")]
    public async Task Client_routes_serve_the_spa(string path) =>
        (await _factory.CreateClient().GetStringAsync(path, TestContext.Current.CancellationToken)).ShouldContain(DashboardFactory.SpaMarker);
}

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PcMonitor.Dashboard.Tests;

public sealed class SpaHostingTests : IAsyncLifetime
{
    private const string SpaMarker = "<title>spa</title>";

    private readonly string _webRoot = Directory.CreateTempSubdirectory("pcmonitor-webroot-").FullName;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public ValueTask InitializeAsync()
    {
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), $"<!doctype html>{SpaMarker}");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseWebRoot(_webRoot));
        _client = _factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        Directory.Delete(_webRoot, recursive: true);
    }

    [Fact]
    public async Task Unknown_api_route_returns_404_not_the_spa()
    {
        var response = await _client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain(SpaMarker);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/incidents")]
    [InlineData("/history")]
    public async Task Client_routes_serve_the_spa(string path) =>
        (await _client.GetStringAsync(path, TestContext.Current.CancellationToken)).ShouldContain(SpaMarker);
}

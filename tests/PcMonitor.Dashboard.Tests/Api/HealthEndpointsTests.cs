using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PcMonitor.Dashboard.Api.Health;

namespace PcMonitor.Dashboard.Tests.Api;

public sealed class HealthEndpointsTests
{
    [Fact]
    public async Task Health_returns_ok_with_version()
    {
        await using var factory = new WebApplicationFactory<Program>();

        var response = await factory.CreateClient()
            .GetFromJsonAsync<HealthResponse>("/api/health", TestContext.Current.CancellationToken);

        response.ShouldNotBeNull();
        response.Status.ShouldBe("ok");
        response.Version.ShouldBe("0.1.0");
    }
}

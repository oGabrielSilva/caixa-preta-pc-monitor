using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PcMonitor.Application.Storage;
using PcMonitor.Dashboard.Api;

namespace PcMonitor.Dashboard.Tests.Api;

public sealed class StorageGateTests
{
    private static async Task<HttpResponseMessage> Probe(StorageState state)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<IStorageStatus>(new FixedStorageStatus(state));
        var app = builder.Build();
        app.MapGroup("/api/probe").RequireStorage().MapGet("/", () => "data");
        await app.StartAsync(TestContext.Current.CancellationToken);

        var response = await app.GetTestClient().GetAsync(new Uri("/api/probe", UriKind.Relative), TestContext.Current.CancellationToken);
        await app.StopAsync(TestContext.Current.CancellationToken);
        return response;
    }

    [Fact]
    public async Task Ready_storage_lets_the_request_through()
    {
        using var response = await Probe(StorageState.Ready);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(StorageState.Missing, "storage.missing")]
    [InlineData(StorageState.SchemaTooOld, "storage.schema_too_old")]
    [InlineData(StorageState.SchemaTooNew, "storage.schema_too_new")]
    [InlineData(StorageState.Unreadable, "storage.unreadable")]
    public async Task Storage_that_is_not_ready_answers_503_with_its_type(StorageState state, string type)
    {
        using var response = await Probe(state);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull().Type.ShouldBe(type);
    }

    private sealed class FixedStorageStatus(StorageState state) : IStorageStatus
    {
        public StorageState Check() => state;
    }
}

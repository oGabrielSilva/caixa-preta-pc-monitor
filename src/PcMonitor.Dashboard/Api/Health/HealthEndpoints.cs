using PcMonitor.Application.Storage;

namespace PcMonitor.Dashboard.Api.Health;

internal static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/health", (IStorageStatus storage) => TypedResults.Ok(new HealthResponse("ok", ApplicationVersion.Current, storage.Check())))
            .WithName("GetHealth")
            .WithTags("Health");

        return api;
    }
}

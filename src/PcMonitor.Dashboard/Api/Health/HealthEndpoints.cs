namespace PcMonitor.Dashboard.Api.Health;

internal static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/health", () => TypedResults.Ok(new HealthResponse("ok", ApplicationVersion.Current)))
            .WithName("GetHealth")
            .WithTags("Health");

        return api;
    }
}

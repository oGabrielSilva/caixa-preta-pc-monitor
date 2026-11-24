using PcMonitor.Dashboard.Api.Health;

namespace PcMonitor.Dashboard.Api;

internal static class ApiEndpoints
{
    public const string Prefix = "/api";

    /// <summary>
    /// Maps every API resource under <see cref="Prefix"/>. Unknown API paths answer 404 here,
    /// so the SPA fallback can never serve <c>index.html</c> for an API call.
    /// </summary>
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(Prefix);

        api.MapHealthEndpoints();
        api.MapFallback(() => TypedResults.NotFound()).ExcludeFromDescription();

        return app;
    }
}

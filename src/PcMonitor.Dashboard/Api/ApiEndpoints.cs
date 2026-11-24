using PcMonitor.Dashboard.Api.Health;

namespace PcMonitor.Dashboard.Api;

internal static class ApiEndpoints
{
    public const string Prefix = "/api";

    /// <summary>Maps every API resource under <see cref="Prefix"/>.</summary>
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder app)
    {
        app.MapGroup(Prefix)
            .MapHealthEndpoints();

        return app;
    }
}

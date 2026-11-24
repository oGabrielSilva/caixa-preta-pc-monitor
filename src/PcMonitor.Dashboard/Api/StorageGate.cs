using PcMonitor.Application.Storage;

namespace PcMonitor.Dashboard.Api;

/// <summary>
/// Answers 503 with a ProblemDetails whose <c>type</c> names the problem (for example <c>storage.missing</c>) when the
/// database cannot be read yet, instead of letting every data endpoint fail on its own.
/// </summary>
internal static class StorageGate
{
    public static RouteGroupBuilder RequireStorage(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(async (context, next) =>
        {
            var state = context.HttpContext.RequestServices.GetRequiredService<IStorageStatus>().Check();
            return state == StorageState.Ready
                ? await next(context).ConfigureAwait(false)
                : TypedResults.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    type: TypeOf(state),
                    title: "The monitoring database is not available.");
        });
        group.WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status503ServiceUnavailable, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), ["application/problem+json"]));
        return group;
    }

    public static string TypeOf(StorageState state) => state switch
    {
        StorageState.Missing => "storage.missing",
        StorageState.SchemaTooOld => "storage.schema_too_old",
        StorageState.SchemaTooNew => "storage.schema_too_new",
        StorageState.Unreadable => "storage.unreadable",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };
}

using Microsoft.AspNetCore.Http.HttpResults;
using PcMonitor.Application.History;

namespace PcMonitor.Dashboard.Api.History;

internal static class HistoryEndpoints
{
    public static RouteGroupBuilder MapHistoryEndpoints(this RouteGroupBuilder api)
    {
        var history = api.MapGroup("/history").WithTags("History").RequireStorage();

        history.MapGet("/sensors", (IHistoryReader reader) =>
                TypedResults.Ok(reader.Sensors().Select(HistoryResponse.Describe).ToList()))
            .WithName("ListHistorySensors");

        history.MapGet("/", Results<Ok<HistoryResponse>, NotFound, ValidationProblem> (string sensor, HistoryRange range, QueryHistory query) =>
            {
                // Enum binding also accepts numbers (?range=7); only the named periods exist.
                if (!Enum.IsDefined(range))
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["range"] = [$"Unknown period '{range}'."] });
                }

                return query.Run(sensor, range) is { } result ? TypedResults.Ok(HistoryResponse.Of(result)) : TypedResults.NotFound();
            })
            .WithName("GetHistory")
            .WithDescription("Min, mean and max of `sensor` (stable key) over `range`, in buckets sized for the period.");

        return api;
    }
}

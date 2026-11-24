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

        history.MapGet("/", Results<Ok<HistoryResponse>, NotFound> (string sensor, HistoryRange range, QueryHistory query) =>
                query.Run(sensor, range) is { } result ? TypedResults.Ok(HistoryResponse.Of(result)) : TypedResults.NotFound())
            .WithName("GetHistory")
            .WithDescription("Min, mean and max of `sensor` (stable key) over `range`, in buckets sized for the period.");

        return api;
    }
}

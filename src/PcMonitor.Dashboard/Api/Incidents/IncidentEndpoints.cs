using Microsoft.AspNetCore.Http.HttpResults;
using PcMonitor.Application.Incidents;

namespace PcMonitor.Dashboard.Api.Incidents;

internal static class IncidentEndpoints
{
    public static RouteGroupBuilder MapIncidentEndpoints(this RouteGroupBuilder api)
    {
        var incidents = api.MapGroup("/incidents").WithTags("Incidents").RequireStorage();

        incidents.MapGet("/", (IIncidentQuery query) =>
                TypedResults.Ok(query.List().Select(IncidentSummaryResponse.From).ToList()))
            .WithName("ListIncidents");

        incidents.MapGet("/{id:long}", Results<Ok<IncidentDetailResponse>, NotFound> (long id, IIncidentQuery query) =>
                query.Find(id) is { } detail ? TypedResults.Ok(IncidentDetailResponse.From(detail)) : TypedResults.NotFound())
            .WithName("GetIncident");

        return api;
    }
}

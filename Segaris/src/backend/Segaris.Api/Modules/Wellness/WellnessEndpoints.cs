using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Wellness.Contracts;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Wellness;

/// <summary>
/// Maps the frozen Wellness HTTP surface. Wave 0 exposes route metadata only; later
/// waves replace the placeholder handlers with the persisted task catalogue, daily
/// generation, scoring, and day-range read behavior. All writes require antiforgery;
/// today, day-task, and day records are always scoped to the current user; and
/// catalogue mutations require administrator authorization.
/// </summary>
internal static class WellnessEndpoints
{
    public static IEndpointRouteBuilder MapWellnessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup(WellnessApiRoutes.Wellness, WellnessApiRoutes.Tag)
            .RequireAuthorization();

        MapTodayEndpoints(group);
        MapDayRangeEndpoints(group);
        MapTaskCatalogueEndpoints(group);

        return endpoints;
    }

    private static void MapTodayEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/today", Placeholder)
            .WithName("GetWellnessToday")
            .WithSummary("Returns the current household day's selected tasks and score for the current user")
            .Produces<WellnessTodayResponse>();

        group.MapPost("/today/tasks/{dayTaskId:int}/toggle", Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ToggleWellnessDayTask")
            .WithSummary("Flips one day-task's completion, recomputes the score, and returns the updated day")
            .Produces<WellnessTodayResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapDayRangeEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/days", Placeholder)
            .WithName("ListWellnessDays")
            .WithSummary("Returns per-day scores for existing days in an inclusive range, current user only")
            .Produces<WellnessDayListResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static void MapTaskCatalogueEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/tasks", Placeholder)
            .WithName("ListWellnessTasks")
            .Produces<IReadOnlyList<WellnessTaskResponse>>();

        var tasks = group.MapGroup("/tasks").RequireAuthorization(IdentityPolicies.Admin);
        tasks.MapPost("", Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateWellnessTask")
            .Produces<WellnessTaskResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        tasks.MapDelete(WellnessApiRoutes.TaskById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteWellnessTask")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static IResult Placeholder() => TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
}

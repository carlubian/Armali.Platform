using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Wellness.Contracts;
using Segaris.Api.Modules.Wellness.Mutations;
using Segaris.Api.Modules.Wellness.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

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
        group.MapGet("/today", GetTodayAsync)
            .WithName("GetWellnessToday")
            .WithSummary("Returns the current household day's selected tasks and score for the current user")
            .Produces<WellnessTodayResponse>();

        group.MapPost("/today/tasks/{dayTaskId:int}/toggle", ToggleDayTaskAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ToggleWellnessDayTask")
            .WithSummary("Flips one day-task's completion, recomputes the score, and returns the updated day")
            .Produces<WellnessTodayResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapDayRangeEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/days", ListDaysAsync)
            .WithName("ListWellnessDays")
            .WithSummary("Returns per-day scores for existing days in an inclusive range, current user only")
            .Produces<WellnessDayListResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static void MapTaskCatalogueEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/tasks", ListTasksAsync)
            .WithName("ListWellnessTasks")
            .WithSummary("Returns the shared task catalogue in creation order")
            .Produces<IReadOnlyList<WellnessTaskResponse>>();

        var tasks = group.MapGroup("/tasks").RequireAuthorization(IdentityPolicies.Admin);
        tasks.MapPost("", CreateTaskAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateWellnessTask")
            .WithSummary("Creates a catalogue task with a name and a fixed category")
            .Produces<WellnessTaskResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        tasks.MapDelete(WellnessApiRoutes.TaskById, DeleteTaskAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteWellnessTask")
            .WithSummary("Deletes a catalogue task; impact-free because days hold task snapshots")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListTasksAsync(
        WellnessTaskReadService read,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListAsync(cancellationToken));

    private static async Task<IResult> GetTodayAsync(
        WellnessDayService service,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await service.GetTodayAsync(userId, cancellationToken));
    }

    private static async Task<IResult> ToggleDayTaskAsync(
        int dayTaskId,
        WellnessDayService service,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await service.ToggleTaskAsync(dayTaskId, userId, cancellationToken));
    }

    private static async Task<IResult> ListDaysAsync(
        DateOnly? from,
        DateOnly? to,
        WellnessDayService service,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await service.ListDaysAsync(from, to, userId, cancellationToken));
    }

    private static async Task<IResult> CreateTaskAsync(
        CreateWellnessTaskRequest request,
        WellnessTaskManagementService service,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(request, currentUser.UserId, cancellationToken);
        return TypedResults.Created($"/api/wellness/tasks/{created.Id}", created);
    }

    private static async Task<IResult> DeleteTaskAsync(
        int taskId,
        WellnessTaskManagementService service,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(taskId, cancellationToken);
        return TypedResults.NoContent();
    }
}

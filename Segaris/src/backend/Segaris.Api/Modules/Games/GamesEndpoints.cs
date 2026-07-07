using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Games;

/// <summary>
/// Maps the frozen Games HTTP surface. Wave 0 exposes route metadata only; later
/// waves replace the placeholder handlers with the persisted game catalogue,
/// playthrough, section, and goal behavior. All writes require antiforgery, and
/// section and goal routes are always scoped through their owning playthrough.
/// </summary>
internal static class GamesEndpoints
{
    public static IEndpointRouteBuilder MapGamesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup(GamesApiRoutes.Games, GamesApiRoutes.Tag)
            .RequireAuthorization();

        MapGameCatalogueEndpoints(group);
        MapPlaythroughEndpoints(group);
        MapSectionEndpoints(group);
        MapGoalEndpoints(group);

        return endpoints;
    }

    private static void MapGameCatalogueEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/games", Placeholder)
            .WithName("ListGames")
            .Produces<IReadOnlyList<GameResponse>>();

        var games = group.MapGroup("/games").RequireAuthorization(IdentityPolicies.Admin);
        games.MapPost("", Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateGame");
        games.MapPut(GamesApiRoutes.GameById, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateGame");
        games.MapPost(GamesApiRoutes.GameMove, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveGame");
        games.MapGet(GamesApiRoutes.GameDeletionImpact, Placeholder).WithName("GetGameDeletionImpact");
        games.MapDelete(GamesApiRoutes.GameById, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteGame");
        games.MapPost(GamesApiRoutes.GameReplaceAndDelete, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteGame");
    }

    private static void MapPlaythroughEndpoints(RouteGroupBuilder group)
    {
        var playthroughs = group.MapGroup("/playthroughs");
        playthroughs.MapGet("", Placeholder)
            .WithName("ListPlaythroughs")
            .Produces<PaginatedResponse<PlaythroughSummaryResponse>>();
        playthroughs.MapPost("", Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreatePlaythrough")
            .Produces<PlaythroughResponse>(StatusCodes.Status201Created);
        playthroughs.MapGet(GamesApiRoutes.PlaythroughById, Placeholder)
            .WithName("GetPlaythrough")
            .Produces<PlaythroughResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        playthroughs.MapPut(GamesApiRoutes.PlaythroughById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdatePlaythrough")
            .Produces<PlaythroughResponse>();
        playthroughs.MapDelete(GamesApiRoutes.PlaythroughById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeletePlaythrough")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapSectionEndpoints(RouteGroupBuilder group)
    {
        var playthroughs = group.MapGroup("/playthroughs");
        playthroughs.MapGet(GamesApiRoutes.Sections, Placeholder)
            .WithName("ListPlaythroughSections")
            .Produces<IReadOnlyList<SectionResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        playthroughs.MapPost(GamesApiRoutes.Sections, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreatePlaythroughSection")
            .Produces<SectionResponse>(StatusCodes.Status201Created);
        playthroughs.MapPut(GamesApiRoutes.SectionsOrder, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ReorderPlaythroughSections")
            .Produces(StatusCodes.Status204NoContent);
        playthroughs.MapGet(GamesApiRoutes.SectionById, Placeholder)
            .WithName("GetPlaythroughSection")
            .Produces<SectionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        playthroughs.MapPut(GamesApiRoutes.SectionById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdatePlaythroughSection")
            .Produces<SectionResponse>();
        playthroughs.MapDelete(GamesApiRoutes.SectionById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeletePlaythroughSection")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapGoalEndpoints(RouteGroupBuilder group)
    {
        var playthroughs = group.MapGroup("/playthroughs");
        playthroughs.MapGet(GamesApiRoutes.Goals, Placeholder)
            .WithName("ListSectionGoals")
            .Produces<IReadOnlyList<GoalResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        playthroughs.MapPost(GamesApiRoutes.Goals, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateSectionGoal")
            .Produces<GoalResponse>(StatusCodes.Status201Created);
        playthroughs.MapPut(GamesApiRoutes.GoalById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateSectionGoal")
            .Produces<GoalResponse>();
        playthroughs.MapPut(GamesApiRoutes.GoalCompletion, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("SetSectionGoalCompletion")
            .Produces<GoalResponse>();
        playthroughs.MapDelete(GamesApiRoutes.GoalById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteSectionGoal")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static IResult Placeholder() => TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
}

using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Api.Modules.Games.Mutations;
using Segaris.Api.Modules.Games.Queries;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

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
        group.MapGet("/games", ListGamesAsync)
            .WithName("ListGames")
            .Produces<IReadOnlyList<GameResponse>>();

        var games = group.MapGroup("/games").RequireAuthorization(IdentityPolicies.Admin);
        games.MapPost("", CreateGameAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateGame").Produces<GameResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        games.MapPut(GamesApiRoutes.GameById, UpdateGameAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateGame").Produces<GameResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        games.MapPost(GamesApiRoutes.GameMove, MoveGameAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveGame").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        games.MapGet(GamesApiRoutes.GameDeletionImpact, GameImpactAsync).WithName("GetGameDeletionImpact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        games.MapDelete(GamesApiRoutes.GameById, DeleteGameAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteGame").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        games.MapPost(GamesApiRoutes.GameReplaceAndDelete, ReplaceAndDeleteGameAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteGame").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapPlaythroughEndpoints(RouteGroupBuilder group)
    {
        var playthroughs = group.MapGroup("/playthroughs");
        playthroughs.MapGet("", ListPlaythroughsAsync)
            .WithName("ListPlaythroughs")
            .WithSummary("Returns a paginated, filtered, and sorted playthrough card collection")
            .Produces<PaginatedResponse<PlaythroughSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        playthroughs.MapPost("", CreatePlaythroughAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreatePlaythrough")
            .WithSummary("Creates a playthrough")
            .Produces<PlaythroughResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        playthroughs.MapGet(GamesApiRoutes.PlaythroughById, GetPlaythroughAsync)
            .WithName("GetPlaythrough")
            .WithSummary("Returns the detail of an accessible playthrough")
            .Produces<PlaythroughResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        playthroughs.MapPut(GamesApiRoutes.PlaythroughById, UpdatePlaythroughAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdatePlaythrough")
            .WithSummary("Updates an accessible playthrough")
            .Produces<PlaythroughResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        playthroughs.MapDelete(GamesApiRoutes.PlaythroughById, DeletePlaythroughAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeletePlaythrough")
            .WithSummary("Deletes an accessible playthrough and its sections and goals")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
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

    private static async Task<IResult> ListGamesAsync(GameReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListAsync(cancellationToken));

    private static async Task<IResult> CreateGameAsync(
        CreateGameRequest request,
        GameManagementService service,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var value = await service.CreateAsync(request, CatalogActor(currentUser), cancellationToken);
        return TypedResults.Created($"/api/games/games/{value.Id}", value);
    }

    private static async Task<IResult> UpdateGameAsync(
        int gameId,
        UpdateGameRequest request,
        GameManagementService service,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(gameId, request, CatalogActor(currentUser), cancellationToken));

    private static async Task<IResult> MoveGameAsync(
        int gameId,
        CatalogMoveRequest request,
        GameManagementService service,
        CancellationToken cancellationToken)
    {
        await service.MoveAsync(gameId, GameDirection(request), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GameImpactAsync(
        int gameId,
        GameManagementService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ImpactAsync(gameId, cancellationToken));

    private static async Task<IResult> DeleteGameAsync(
        int gameId,
        GameManagementService service,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(gameId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteGameAsync(
        int gameId,
        CatalogReplacementRequest request,
        GameManagementService service,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await service.ReplaceAndDeleteAsync(gameId, request, CatalogActor(currentUser), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListPlaythroughsAsync(
        [AsParameters] PlaythroughListQuery query,
        PlaythroughReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await read.ListPlaythroughsAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken));
    }

    private static async Task<IResult> GetPlaythroughAsync(
        int playthroughId,
        PlaythroughReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var playthrough = await read.GetPlaythroughAsync(playthroughId, userId, cancellationToken);
        return playthrough is null ? throw PlaythroughProblem.NotFound() : TypedResults.Ok(playthrough);
    }

    private static async Task<IResult> CreatePlaythroughAsync(
        CreatePlaythroughRequest request,
        PlaythroughWriteService write,
        PlaythroughReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int playthroughId;
        try
        {
            playthroughId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (GamesValidationException exception)
        {
            throw PlaythroughProblem.From(exception);
        }

        var playthrough = await read.GetPlaythroughAsync(playthroughId, userId, cancellationToken);
        return TypedResults.Created($"/api/games/playthroughs/{playthroughId}", playthrough);
    }

    private static async Task<IResult> UpdatePlaythroughAsync(
        int playthroughId,
        UpdatePlaythroughRequest request,
        PlaythroughWriteService write,
        PlaythroughReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool updated;
        try
        {
            updated = await write.UpdateAsync(playthroughId, request, userId, cancellationToken);
        }
        catch (GamesValidationException exception)
        {
            throw PlaythroughProblem.From(exception);
        }

        if (!updated)
        {
            throw PlaythroughProblem.NotFound();
        }

        var playthrough = await read.GetPlaythroughAsync(playthroughId, userId, cancellationToken);
        return TypedResults.Ok(playthrough);
    }

    private static async Task<IResult> DeletePlaythroughAsync(
        int playthroughId,
        PlaythroughWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(playthroughId, userId, cancellationToken);
        if (!deleted)
        {
            throw PlaythroughProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static UserId CatalogActor(ICurrentUser currentUser) =>
        currentUser.UserId ?? throw GameProblem.NotFound();

    private static CatalogMoveDirection GameDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw GameProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static IResult Placeholder() => TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
}

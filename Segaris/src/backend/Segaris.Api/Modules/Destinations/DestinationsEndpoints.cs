using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Api.Modules.Destinations.Mutations;
using Segaris.Api.Modules.Destinations.Queries;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Destinations;

/// <summary>
/// Maps the Destinations HTTP surface. Wave 2 exposes destination reads and
/// mutations alongside the two module-owned category catalogues surfaced through
/// Configuration; Wave 3 adds the destination-scoped place sub-resource; later waves
/// add attachment behaviour. State-changing routes carry antiforgery protection and
/// never expose EF Core entities.
/// </summary>
internal static class DestinationsEndpoints
{
    public static IEndpointRouteBuilder MapDestinationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup(DestinationsApiRoutes.Destinations, DestinationsApiRoutes.Tag)
            .RequireAuthorization();

        MapDestinationEndpoints(group);
        MapPlaceEndpoints(group);
        MapCategoryEndpoints(group);
        MapPlaceCategoryEndpoints(group);

        return endpoints;
    }

    private static void MapDestinationEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("", ListDestinationsAsync)
            .WithName("ListDestinations")
            .WithSummary("Returns a paginated, filtered, and sorted destination gallery")
            .Produces<PaginatedResponse<DestinationSummaryResponse>>();

        group.MapGet(DestinationsApiRoutes.DestinationById, GetDestinationAsync)
            .WithName("GetDestination")
            .WithSummary("Returns the detail of an accessible destination")
            .Produces<DestinationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("", CreateDestinationAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateDestination")
            .WithSummary("Creates a destination")
            .Produces<DestinationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut(DestinationsApiRoutes.DestinationById, UpdateDestinationAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateDestination")
            .WithSummary("Updates an accessible destination")
            .Produces<DestinationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete(DestinationsApiRoutes.DestinationById, DeleteDestinationAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteDestination")
            .WithSummary("Deletes an accessible destination and its places")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapPlaceEndpoints(RouteGroupBuilder group)
    {
        group.MapGet(DestinationsApiRoutes.Places, ListPlacesAsync)
            .WithName("ListPlaces")
            .WithSummary("Returns a paginated, filtered, and sorted list of a destination's places")
            .Produces<PaginatedResponse<PlaceSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet(DestinationsApiRoutes.PlaceById, GetPlaceAsync)
            .WithName("GetPlace")
            .WithSummary("Returns the detail of a place within an accessible destination")
            .Produces<PlaceSummaryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(DestinationsApiRoutes.Places, CreatePlaceAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreatePlace")
            .WithSummary("Creates a place within an accessible destination")
            .Produces<PlaceSummaryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(DestinationsApiRoutes.PlaceById, UpdatePlaceAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdatePlace")
            .WithSummary("Updates a place within an accessible destination")
            .Produces<PlaceSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete(DestinationsApiRoutes.PlaceById, DeletePlaceAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeletePlace")
            .WithSummary("Deletes a place within an accessible destination")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListDestinationCategories")
            .WithSummary("Returns the Destinations destination category catalogue")
            .Produces<IReadOnlyList<DestinationCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateDestinationCategory")
            .WithSummary("Creates a destination category at the end of the catalogue")
            .Produces<DestinationCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(DestinationsApiRoutes.CategoryById, UpdateCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateDestinationCategory")
            .WithSummary("Updates a destination category")
            .Produces<DestinationCategoryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(DestinationsApiRoutes.CategoryMove, MoveCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("MoveDestinationCategory")
            .WithSummary("Moves a destination category one position")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(DestinationsApiRoutes.CategoryDeletionImpact, CategoryImpactAsync)
            .WithName("GetDestinationCategoryDeletionImpact")
            .WithSummary("Returns privacy-neutral destination category deletion impact")
            .Produces<CatalogDeletionImpactResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(DestinationsApiRoutes.CategoryById, DeleteCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteDestinationCategory")
            .WithSummary("Deletes an unreferenced destination category")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(DestinationsApiRoutes.CategoryReplaceAndDelete, ReplaceAndDeleteCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ReplaceAndDeleteDestinationCategory")
            .WithSummary("Migrates references and deletes a destination category atomically")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapPlaceCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/place-categories", ListPlaceCategoriesAsync)
            .WithName("ListPlaceCategories")
            .WithSummary("Returns the Destinations place category catalogue")
            .Produces<IReadOnlyList<PlaceCategoryResponse>>();

        var placeCategories = group.MapGroup("/place-categories").RequireAuthorization(IdentityPolicies.Admin);
        placeCategories.MapPost("", CreatePlaceCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreatePlaceCategory")
            .WithSummary("Creates a place category at the end of the catalogue")
            .Produces<PlaceCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        placeCategories.MapPut(DestinationsApiRoutes.PlaceCategoryById, UpdatePlaceCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdatePlaceCategory")
            .WithSummary("Updates a place category")
            .Produces<PlaceCategoryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        placeCategories.MapPost(DestinationsApiRoutes.PlaceCategoryMove, MovePlaceCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("MovePlaceCategory")
            .WithSummary("Moves a place category one position")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        placeCategories.MapGet(DestinationsApiRoutes.PlaceCategoryDeletionImpact, PlaceCategoryImpactAsync)
            .WithName("GetPlaceCategoryDeletionImpact")
            .WithSummary("Returns privacy-neutral place category deletion impact")
            .Produces<CatalogDeletionImpactResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        placeCategories.MapDelete(DestinationsApiRoutes.PlaceCategoryById, DeletePlaceCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeletePlaceCategory")
            .WithSummary("Deletes an unreferenced place category")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        placeCategories.MapPost(DestinationsApiRoutes.PlaceCategoryReplaceAndDelete, ReplaceAndDeletePlaceCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ReplaceAndDeletePlaceCategory")
            .WithSummary("Migrates references and deletes a place category atomically")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static UserId CategoryActor(ICurrentUser currentUser) =>
        currentUser.UserId ?? throw DestinationCategoryProblem.NotFound();

    private static UserId PlaceCategoryActor(ICurrentUser currentUser) =>
        currentUser.UserId ?? throw PlaceCategoryProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw DestinationCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection PlaceCategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw PlaceCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> ListCategoriesAsync(DestinationsCatalogReadService read, CancellationToken token) =>
        TypedResults.Ok(await read.ListCategoriesAsync(token));

    private static async Task<IResult> ListPlaceCategoriesAsync(DestinationsCatalogReadService read, CancellationToken token) =>
        TypedResults.Ok(await read.ListPlaceCategoriesAsync(token));

    private static async Task<IResult> ListDestinationsAsync(
        [AsParameters] DestinationListQuery query,
        DestinationsReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await read.ListDestinationsAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken));
    }

    private static async Task<IResult> GetDestinationAsync(
        int destinationId,
        DestinationsReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var destination = await read.GetDestinationAsync(destinationId, userId, cancellationToken);
        if (destination is null)
        {
            throw DestinationProblem.NotFound();
        }

        return TypedResults.Ok(destination);
    }

    private static async Task<IResult> CreateDestinationAsync(
        CreateDestinationRequest request,
        DestinationWriteService write,
        DestinationsReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int destinationId;
        try
        {
            destinationId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (DestinationsValidationException exception)
        {
            throw DestinationProblem.From(exception);
        }

        var destination = await read.GetDestinationAsync(destinationId, userId, cancellationToken);
        return TypedResults.Created($"/api/destinations/{destinationId}", destination);
    }

    private static async Task<IResult> UpdateDestinationAsync(
        int destinationId,
        UpdateDestinationRequest request,
        DestinationWriteService write,
        DestinationsReadService read,
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
            updated = await write.UpdateAsync(destinationId, request, userId, cancellationToken);
        }
        catch (DestinationsValidationException exception)
        {
            throw DestinationProblem.From(exception);
        }

        if (!updated)
        {
            throw DestinationProblem.NotFound();
        }

        var destination = await read.GetDestinationAsync(destinationId, userId, cancellationToken);
        return TypedResults.Ok(destination);
    }

    private static async Task<IResult> DeleteDestinationAsync(
        int destinationId,
        DestinationWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(destinationId, userId, cancellationToken);
        if (!deleted)
        {
            throw DestinationProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListPlacesAsync(
        int destinationId,
        [AsParameters] PlaceListQuery query,
        PlaceReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var page = await read.ListPlacesAsync(
            destinationId,
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        if (page is null)
        {
            throw DestinationProblem.NotFound();
        }

        return TypedResults.Ok(page);
    }

    private static async Task<IResult> GetPlaceAsync(
        int destinationId,
        int placeId,
        PlaceReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var place = await read.GetPlaceAsync(destinationId, placeId, userId, cancellationToken);
        if (place is null)
        {
            throw PlaceProblem.NotFound();
        }

        return TypedResults.Ok(place);
    }

    private static async Task<IResult> CreatePlaceAsync(
        int destinationId,
        CreatePlaceRequest request,
        PlaceWriteService write,
        PlaceReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int? placeId;
        try
        {
            placeId = await write.CreateAsync(destinationId, request, userId, cancellationToken);
        }
        catch (DestinationsValidationException exception)
        {
            throw PlaceProblem.From(exception);
        }

        if (placeId is not { } createdId)
        {
            throw DestinationProblem.NotFound();
        }

        var place = await read.GetPlaceAsync(destinationId, createdId, userId, cancellationToken);
        return TypedResults.Created($"/api/destinations/{destinationId}/places/{createdId}", place);
    }

    private static async Task<IResult> UpdatePlaceAsync(
        int destinationId,
        int placeId,
        UpdatePlaceRequest request,
        PlaceWriteService write,
        PlaceReadService read,
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
            updated = await write.UpdateAsync(destinationId, placeId, request, userId, cancellationToken);
        }
        catch (DestinationsValidationException exception)
        {
            throw PlaceProblem.From(exception);
        }

        if (!updated)
        {
            throw PlaceProblem.NotFound();
        }

        var place = await read.GetPlaceAsync(destinationId, placeId, userId, cancellationToken);
        return TypedResults.Ok(place);
    }

    private static async Task<IResult> DeletePlaceAsync(
        int destinationId,
        int placeId,
        PlaceWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(destinationId, placeId, userId, cancellationToken);
        if (!deleted)
        {
            throw PlaceProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateCategoryAsync(
        DestinationCategoryRequest request, DestinationCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CategoryActor(user), token);
        return TypedResults.Created($"/api/destinations/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(
        int categoryId, DestinationCategoryRequest request, DestinationCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CategoryActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(
        int categoryId, CatalogMoveRequest request, DestinationCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(
        int categoryId, DestinationCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(
        int categoryId, DestinationCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteCategoryAsync(
        int categoryId, CatalogReplacementRequest request, DestinationCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, CategoryActor(user), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreatePlaceCategoryAsync(
        PlaceCategoryRequest request, PlaceCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, PlaceCategoryActor(user), token);
        return TypedResults.Created($"/api/destinations/place-categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdatePlaceCategoryAsync(
        int placeCategoryId, PlaceCategoryRequest request, PlaceCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(placeCategoryId, request, PlaceCategoryActor(user), token));

    private static async Task<IResult> MovePlaceCategoryAsync(
        int placeCategoryId, CatalogMoveRequest request, PlaceCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(placeCategoryId, PlaceCategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> PlaceCategoryImpactAsync(
        int placeCategoryId, PlaceCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(placeCategoryId, token));

    private static async Task<IResult> DeletePlaceCategoryAsync(
        int placeCategoryId, PlaceCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(placeCategoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeletePlaceCategoryAsync(
        int placeCategoryId, CatalogReplacementRequest request, PlaceCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(placeCategoryId, request, PlaceCategoryActor(user), token);
        return TypedResults.NoContent();
    }
}

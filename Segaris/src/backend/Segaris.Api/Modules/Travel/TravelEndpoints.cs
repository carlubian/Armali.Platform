using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Mutations;
using Segaris.Api.Modules.Travel.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Travel;

/// <summary>
/// Maps the Travel HTTP surface. Wave 1 exposes the module-owned trip-type and
/// expense-category catalog reads and the administrator-only catalog management
/// routes surfaced through Configuration; later waves add the trip read, mutation,
/// itinerary, attachment, and expense sub-resource routes frozen in
/// <see cref="TravelApiRoutes"/>. State-changing routes carry antiforgery protection
/// and never expose EF Core entities.
/// </summary>
internal static class TravelEndpoints
{
    public static IEndpointRouteBuilder MapTravelEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("travel", TravelApiRoutes.Tag)
            .RequireAuthorization();

        MapTripEndpoints(group);
        MapTripTypeEndpoints(group);
        MapExpenseCategoryEndpoints(group);

        return endpoints;
    }

    private static void MapTripEndpoints(RouteGroupBuilder group)
    {
        var trips = group.MapGroup("/trips");

        trips.MapGet("", ListTripsAsync)
            .WithName("ListTravelTrips")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible Travel trips")
            .Produces<PaginatedResponse<TravelTripSummaryResponse>>();

        trips.MapGet(TravelApiRoutes.TripById, GetTripAsync)
            .WithName("GetTravelTrip")
            .WithSummary("Returns the detail of an accessible Travel trip with itinerary and per-currency totals")
            .Produces<TravelTripResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapTripTypeEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/trip-types", ListTripTypesAsync)
            .WithName("ListTravelTripTypes")
            .WithSummary("Returns the Travel trip type catalog")
            .Produces<IReadOnlyList<TravelTripTypeResponse>>();

        var tripTypes = group.MapGroup("/trip-types").RequireAuthorization(IdentityPolicies.Admin);
        tripTypes.MapPost("", CreateTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateTravelTripType").WithSummary("Creates a trip type at the end of the catalog").Produces<TravelTripTypeResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        tripTypes.MapPut(TravelApiRoutes.TripTypeById, UpdateTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateTravelTripType").WithSummary("Updates a Travel trip type").Produces<TravelTripTypeResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        tripTypes.MapPost(TravelApiRoutes.TripTypeMove, MoveTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveTravelTripType").WithSummary("Moves a Travel trip type one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        tripTypes.MapGet(TravelApiRoutes.TripTypeDeletionImpact, TripTypeImpactAsync).WithName("GetTravelTripTypeDeletionImpact").WithSummary("Returns privacy-neutral trip type deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        tripTypes.MapDelete(TravelApiRoutes.TripTypeById, DeleteTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteTravelTripType").WithSummary("Deletes an unreferenced Travel trip type").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        tripTypes.MapPost(TravelApiRoutes.TripTypeReplaceAndDelete, ReplaceAndDeleteTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteTravelTripType").WithSummary("Migrates references and deletes a Travel trip type atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapExpenseCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/expense-categories", ListExpenseCategoriesAsync)
            .WithName("ListTravelExpenseCategories")
            .WithSummary("Returns the Travel expense category catalog")
            .Produces<IReadOnlyList<TravelExpenseCategoryResponse>>();

        var categories = group.MapGroup("/expense-categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateTravelExpenseCategory").WithSummary("Creates an expense category at the end of the catalog").Produces<TravelExpenseCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(TravelApiRoutes.ExpenseCategoryById, UpdateExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateTravelExpenseCategory").WithSummary("Updates a Travel expense category").Produces<TravelExpenseCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(TravelApiRoutes.ExpenseCategoryMove, MoveExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveTravelExpenseCategory").WithSummary("Moves a Travel expense category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(TravelApiRoutes.ExpenseCategoryDeletionImpact, ExpenseCategoryImpactAsync).WithName("GetTravelExpenseCategoryDeletionImpact").WithSummary("Returns privacy-neutral expense category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(TravelApiRoutes.ExpenseCategoryById, DeleteExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteTravelExpenseCategory").WithSummary("Deletes an unreferenced Travel expense category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(TravelApiRoutes.ExpenseCategoryReplaceAndDelete, ReplaceAndDeleteExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteTravelExpenseCategory").WithSummary("Migrates references and deletes a Travel expense category atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListTripTypesAsync(TravelReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListTripTypesAsync(cancellationToken));

    private static async Task<IResult> ListExpenseCategoriesAsync(TravelReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListExpenseCategoriesAsync(cancellationToken));

    private static async Task<IResult> ListTripsAsync(
        [AsParameters] TravelTripListQuery query,
        TravelReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListTripsAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetTripAsync(
        int tripId,
        TravelReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var trip = await read.GetTripAsync(tripId, userId, cancellationToken);
        if (trip is null)
        {
            throw TravelTripProblem.NotFound();
        }

        return TypedResults.Ok(trip);
    }

    private static UserId TripTypeActor(ICurrentUser currentUser) => currentUser.UserId ?? throw TravelTripTypeProblem.NotFound();

    private static UserId ExpenseCategoryActor(ICurrentUser currentUser) => currentUser.UserId ?? throw TravelExpenseCategoryProblem.NotFound();

    private static CatalogMoveDirection TripTypeDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw TravelTripTypeProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection ExpenseCategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw TravelExpenseCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateTripTypeAsync(TravelTripTypeRequest request, TravelTripTypeManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, TripTypeActor(user), token);
        return TypedResults.Created($"/api/travel/trip-types/{value.Id}", value);
    }

    private static async Task<IResult> UpdateTripTypeAsync(int tripTypeId, TravelTripTypeRequest request, TravelTripTypeManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(tripTypeId, request, TripTypeActor(user), token));

    private static async Task<IResult> MoveTripTypeAsync(int tripTypeId, CatalogMoveRequest request, TravelTripTypeManagementService service, CancellationToken token)
    {
        await service.MoveAsync(tripTypeId, TripTypeDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> TripTypeImpactAsync(int tripTypeId, TravelTripTypeManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(tripTypeId, token));

    private static async Task<IResult> DeleteTripTypeAsync(int tripTypeId, TravelTripTypeManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(tripTypeId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteTripTypeAsync(int tripTypeId, CatalogReplacementRequest request, TravelTripTypeManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(tripTypeId, request, TripTypeActor(user), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateExpenseCategoryAsync(TravelExpenseCategoryRequest request, TravelExpenseCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, ExpenseCategoryActor(user), token);
        return TypedResults.Created($"/api/travel/expense-categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateExpenseCategoryAsync(int expenseCategoryId, TravelExpenseCategoryRequest request, TravelExpenseCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(expenseCategoryId, request, ExpenseCategoryActor(user), token));

    private static async Task<IResult> MoveExpenseCategoryAsync(int expenseCategoryId, CatalogMoveRequest request, TravelExpenseCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(expenseCategoryId, ExpenseCategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ExpenseCategoryImpactAsync(int expenseCategoryId, TravelExpenseCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(expenseCategoryId, token));

    private static async Task<IResult> DeleteExpenseCategoryAsync(int expenseCategoryId, TravelExpenseCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(expenseCategoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteExpenseCategoryAsync(int expenseCategoryId, CatalogReplacementRequest request, TravelExpenseCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(expenseCategoryId, request, ExpenseCategoryActor(user), token);
        return TypedResults.NoContent();
    }
}

using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Mutations;
using Segaris.Api.Modules.Inventory.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory;

/// <summary>
/// Maps the Inventory HTTP surface. Wave 1 exposes the module-owned category and
/// location catalog reads and the administrator-only catalog management routes
/// surfaced through Configuration; later Waves add the item and order read,
/// mutation, attachment, and receive routes frozen in <see cref="InventoryApiRoutes"/>.
/// State-changing routes carry antiforgery protection and never expose EF Core
/// entities.
/// </summary>
internal static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("inventory", InventoryApiRoutes.Tag)
            .RequireAuthorization();

        MapCategoryEndpoints(group);
        MapLocationEndpoints(group);
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListInventoryCategories")
            .WithSummary("Returns the Inventory category catalog")
            .Produces<IReadOnlyList<InventoryCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateInventoryCategory").WithSummary("Creates a category at the end of the catalog").Produces<InventoryCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(InventoryApiRoutes.CategoryById, UpdateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateInventoryCategory").WithSummary("Updates an Inventory category").Produces<InventoryCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(InventoryApiRoutes.CategoryMove, MoveCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveInventoryCategory").WithSummary("Moves an Inventory category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(InventoryApiRoutes.CategoryDeletionImpact, CategoryImpactAsync).WithName("GetInventoryCategoryDeletionImpact").WithSummary("Returns privacy-neutral category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(InventoryApiRoutes.CategoryById, DeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteInventoryCategory").WithSummary("Deletes an unreferenced Inventory category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(InventoryApiRoutes.CategoryReplaceAndDelete, ReplaceAndDeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteInventoryCategory").WithSummary("Migrates references and deletes an Inventory category atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapLocationEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/locations", ListLocationsAsync)
            .WithName("ListInventoryLocations")
            .WithSummary("Returns the Inventory location catalog")
            .Produces<IReadOnlyList<InventoryLocationResponse>>();

        var locations = group.MapGroup("/locations").RequireAuthorization(IdentityPolicies.Admin);
        locations.MapPost("", CreateLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateInventoryLocation").WithSummary("Creates a location at the end of the catalog").Produces<InventoryLocationResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        locations.MapPut(InventoryApiRoutes.LocationById, UpdateLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateInventoryLocation").WithSummary("Updates an Inventory location").Produces<InventoryLocationResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        locations.MapPost(InventoryApiRoutes.LocationMove, MoveLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveInventoryLocation").WithSummary("Moves an Inventory location one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        locations.MapGet(InventoryApiRoutes.LocationDeletionImpact, LocationImpactAsync).WithName("GetInventoryLocationDeletionImpact").WithSummary("Returns privacy-neutral location deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        locations.MapDelete(InventoryApiRoutes.LocationById, DeleteLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteInventoryLocation").WithSummary("Deletes an unreferenced Inventory location").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        locations.MapPost(InventoryApiRoutes.LocationReplaceAndDelete, ReplaceAndDeleteLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteInventoryLocation").WithSummary("Migrates references and deletes an Inventory location atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListCategoriesAsync(InventoryReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListCategoriesAsync(cancellationToken));

    private static async Task<IResult> ListLocationsAsync(InventoryReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListLocationsAsync(cancellationToken));

    private static UserId CatalogActor(ICurrentUser currentUser) => currentUser.UserId ?? throw InventoryCategoryProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw InventoryCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection LocationDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw InventoryLocationProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(CatalogItemRequest request, InventoryCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/inventory/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(int categoryId, CatalogItemRequest request, InventoryCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(int categoryId, CatalogMoveRequest request, InventoryCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(int categoryId, InventoryCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(int categoryId, InventoryCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteCategoryAsync(int categoryId, CatalogReplacementRequest request, InventoryCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateLocationAsync(CatalogItemRequest request, InventoryLocationManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/inventory/locations/{value.Id}", value);
    }

    private static async Task<IResult> UpdateLocationAsync(int locationId, CatalogItemRequest request, InventoryLocationManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(locationId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveLocationAsync(int locationId, CatalogMoveRequest request, InventoryLocationManagementService service, CancellationToken token)
    {
        await service.MoveAsync(locationId, LocationDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> LocationImpactAsync(int locationId, InventoryLocationManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(locationId, token));

    private static async Task<IResult> DeleteLocationAsync(int locationId, InventoryLocationManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(locationId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteLocationAsync(int locationId, CatalogReplacementRequest request, InventoryLocationManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(locationId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }
}

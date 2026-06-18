using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Clothes.Mutations;
using Segaris.Api.Modules.Clothes.Queries;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Clothes;

/// <summary>
/// Maps the Clothes HTTP surface. Wave 0 froze the routes as placeholders; Wave 1
/// exposes the module-owned category and colour catalog reads and the
/// administrator-only catalog management routes surfaced through Configuration. The
/// garment, attachment, and reference-migrating replace-and-delete routes remain
/// placeholders until the later waves that implement them. State-changing routes
/// carry antiforgery protection and never expose EF Core entities.
/// </summary>
internal static class ClothesEndpoints
{
    public static IEndpointRouteBuilder MapClothesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("clothes", ClothesApiRoutes.Tag)
            .RequireAuthorization();

        MapGarmentEndpoints(group);
        MapCategoryEndpoints(group);
        MapColorEndpoints(group);

        return endpoints;
    }

    private static void MapGarmentEndpoints(RouteGroupBuilder group)
    {
        var garments = group.MapGroup("/garments");
        garments.MapGet("", NotImplemented).WithName("ListClothesGarments");
        garments.MapPost("", NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateClothesGarment");
        garments.MapGet(ClothesApiRoutes.GarmentById, NotImplemented).WithName("GetClothesGarment");
        garments.MapPut(ClothesApiRoutes.GarmentById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateClothesGarment");
        garments.MapDelete(ClothesApiRoutes.GarmentById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteClothesGarment");
        garments.MapGet(ClothesApiRoutes.GarmentAttachments, NotImplemented).WithName("ListClothesGarmentAttachments");
        garments.MapPost(ClothesApiRoutes.GarmentAttachments, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UploadClothesGarmentAttachment");
        garments.MapGet(ClothesApiRoutes.GarmentAttachmentById, NotImplemented).WithName("DownloadClothesGarmentAttachment");
        garments.MapDelete(ClothesApiRoutes.GarmentAttachmentById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteClothesGarmentAttachment");
        garments.MapPut(ClothesApiRoutes.GarmentPrimaryAttachment, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("SetClothesGarmentPrimaryAttachment");
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListClothingCategories")
            .WithSummary("Returns the Clothes category catalog")
            .Produces<IReadOnlyList<ClothingCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateClothingCategory").WithSummary("Creates a category at the end of the catalog").Produces<ClothingCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(ClothesApiRoutes.CategoryById, UpdateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateClothingCategory").WithSummary("Updates a Clothes category").Produces<ClothingCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(ClothesApiRoutes.CategoryMove, MoveCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveClothingCategory").WithSummary("Moves a Clothes category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(ClothesApiRoutes.CategoryDeletionImpact, CategoryImpactAsync).WithName("GetClothingCategoryDeletionImpact").WithSummary("Returns privacy-neutral category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(ClothesApiRoutes.CategoryById, DeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteClothingCategory").WithSummary("Deletes an unreferenced Clothes category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        // Reference-migrating deletion is delivered with the Configuration reference handlers in a later wave.
        categories.MapPost(ClothesApiRoutes.CategoryReplaceAndDelete, NotImplemented).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteClothingCategory");
    }

    private static void MapColorEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/colors", ListColorsAsync)
            .WithName("ListClothingColors")
            .WithSummary("Returns the Clothes colour catalog with each colour value")
            .Produces<IReadOnlyList<ClothingColorResponse>>();

        var colors = group.MapGroup("/colors").RequireAuthorization(IdentityPolicies.Admin);
        colors.MapPost("", CreateColorAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateClothingColor").WithSummary("Creates a colour at the end of the catalog").Produces<ClothingColorResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        colors.MapPut(ClothesApiRoutes.ColorById, UpdateColorAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateClothingColor").WithSummary("Updates a Clothes colour and its colour value").Produces<ClothingColorResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        colors.MapPost(ClothesApiRoutes.ColorMove, MoveColorAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveClothingColor").WithSummary("Moves a Clothes colour one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        colors.MapGet(ClothesApiRoutes.ColorDeletionImpact, ColorImpactAsync).WithName("GetClothingColorDeletionImpact").WithSummary("Returns privacy-neutral colour deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        colors.MapDelete(ClothesApiRoutes.ColorById, DeleteColorAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteClothingColor").WithSummary("Deletes an unreferenced Clothes colour").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        // Reference-migrating deletion is delivered with the Configuration reference handlers in a later wave.
        colors.MapPost(ClothesApiRoutes.ColorReplaceAndDelete, NotImplemented).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteClothingColor");
    }

    private static async Task<IResult> ListCategoriesAsync(ClothesReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListCategoriesAsync(cancellationToken));

    private static async Task<IResult> ListColorsAsync(ClothesReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListColorsAsync(cancellationToken));

    private static UserId CategoryActor(ICurrentUser currentUser) => currentUser.UserId ?? throw ClothesCategoryProblem.NotFound();

    private static UserId ColorActor(ICurrentUser currentUser) => currentUser.UserId ?? throw ClothesColorProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw ClothesCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection ColorDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw ClothesColorProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(CatalogItemRequest request, ClothingCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CategoryActor(user), token);
        return TypedResults.Created($"/api/clothes/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(int categoryId, CatalogItemRequest request, ClothingCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CategoryActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(int categoryId, CatalogMoveRequest request, ClothingCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(int categoryId, ClothingCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(int categoryId, ClothingCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateColorAsync(CatalogItemRequest request, ClothingColorManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, ColorActor(user), token);
        return TypedResults.Created($"/api/clothes/colors/{value.Id}", value);
    }

    private static async Task<IResult> UpdateColorAsync(int colorId, CatalogItemRequest request, ClothingColorManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(colorId, request, ColorActor(user), token));

    private static async Task<IResult> MoveColorAsync(int colorId, CatalogMoveRequest request, ClothingColorManagementService service, CancellationToken token)
    {
        await service.MoveAsync(colorId, ColorDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ColorImpactAsync(int colorId, ClothingColorManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(colorId, token));

    private static async Task<IResult> DeleteColorAsync(int colorId, ClothingColorManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(colorId, token);
        return TypedResults.NoContent();
    }

    private static IResult NotImplemented() =>
        Results.StatusCode(StatusCodes.Status501NotImplemented);
}

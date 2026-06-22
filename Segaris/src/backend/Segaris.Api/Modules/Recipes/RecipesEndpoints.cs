using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Mutations;
using Segaris.Api.Modules.Recipes.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Recipes;

/// <summary>
/// Maps the Recipes HTTP surface. Wave 0 froze the route shapes and registered
/// placeholder handlers; Wave 1 replaces the category placeholder and adds the full
/// module-owned category catalog read and administrator management endpoints surfaced
/// through Configuration; later waves replace the remaining placeholders with recipe,
/// menu, and attachment behaviour. State-changing routes carry antiforgery protection
/// and never expose EF Core entities.
/// </summary>
internal static class RecipesEndpoints
{
    public static IEndpointRouteBuilder MapRecipesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("recipes", RecipesApiRoutes.Tag)
            .RequireAuthorization();

        // The recipe collection lives at the module group root (/api/recipes).
        group.MapGet("", NotImplemented).WithName("ListRecipes");
        group.MapPost("", NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateRecipe");
        group.MapGet(RecipesApiRoutes.RecipeById, NotImplemented).WithName("GetRecipe");
        group.MapPut(RecipesApiRoutes.RecipeById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateRecipe");
        group.MapDelete(RecipesApiRoutes.RecipeById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteRecipe");

        group.MapGet(RecipesApiRoutes.RecipeAttachments, NotImplemented).WithName("ListRecipeAttachments");
        group.MapPost(RecipesApiRoutes.RecipeAttachments, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UploadRecipeAttachment");
        group.MapGet(RecipesApiRoutes.RecipeAttachmentById, NotImplemented).WithName("DownloadRecipeAttachment");
        group.MapDelete(RecipesApiRoutes.RecipeAttachmentById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteRecipeAttachment");
        group.MapPut(RecipesApiRoutes.RecipePrimaryAttachment, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("SetRecipePrimaryAttachment");

        var menus = group.MapGroup("/menus");
        menus.MapGet("", NotImplemented).WithName("ListWeeklyMenus");
        menus.MapPost("", NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateWeeklyMenu");
        menus.MapGet(RecipesApiRoutes.MenuById, NotImplemented).WithName("GetWeeklyMenu");
        menus.MapPut(RecipesApiRoutes.MenuById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateWeeklyMenu");
        menus.MapDelete(RecipesApiRoutes.MenuById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteWeeklyMenu");

        MapCategoryEndpoints(group);

        return endpoints;
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListRecipeCategories")
            .WithSummary("Returns the Recipes category catalog")
            .Produces<IReadOnlyList<RecipeCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateRecipeCategory")
            .WithSummary("Creates a recipe category at the end of the catalog")
            .Produces<RecipeCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(RecipesApiRoutes.CategoryById, UpdateCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateRecipeCategory")
            .WithSummary("Updates a recipe category")
            .Produces<RecipeCategoryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(RecipesApiRoutes.CategoryMove, MoveCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("MoveRecipeCategory")
            .WithSummary("Moves a recipe category one position")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(RecipesApiRoutes.CategoryDeletionImpact, CategoryImpactAsync)
            .WithName("GetRecipeCategoryDeletionImpact")
            .WithSummary("Returns privacy-neutral recipe category deletion impact")
            .Produces<CatalogDeletionImpactResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(RecipesApiRoutes.CategoryById, DeleteCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteRecipeCategory")
            .WithSummary("Deletes an unreferenced recipe category")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(RecipesApiRoutes.CategoryReplaceAndDelete, ReplaceAndDeleteCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ReplaceAndDeleteRecipeCategory")
            .WithSummary("Migrates references and deletes a recipe category atomically")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListCategoriesAsync(RecipesReadService read, CancellationToken token) =>
        TypedResults.Ok(await read.ListCategoriesAsync(token));

    private static UserId CatalogActor(ICurrentUser currentUser) =>
        currentUser.UserId ?? throw RecipesCategoryProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw RecipesCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(
        CatalogItemRequest request,
        RecipesCategoryManagementService service,
        ICurrentUser user,
        CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/recipes/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(
        int categoryId, CatalogItemRequest request, RecipesCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(
        int categoryId, CatalogMoveRequest request, RecipesCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(
        int categoryId, RecipesCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(
        int categoryId, RecipesCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteCategoryAsync(
        int categoryId, CatalogReplacementRequest request, RecipesCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }

    private static IResult NotImplemented() =>
        Results.StatusCode(StatusCodes.Status501NotImplemented);
}

using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Api.Modules.Recipes.Mutations;
using Segaris.Api.Modules.Recipes.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
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

        group.MapGet("", ListRecipesAsync)
            .WithName("ListRecipes")
            .WithSummary("Returns a paginated, filtered, and sorted gallery of accessible recipes")
            .Produces<PaginatedResponse<RecipeSummaryResponse>>();
        group.MapPost("", CreateRecipeAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateRecipe")
            .WithSummary("Creates a recipe with free-text ingredients and preparation steps")
            .Produces<RecipeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet(RecipesApiRoutes.RecipeById, GetRecipeAsync)
            .WithName("GetRecipe")
            .WithSummary("Returns the detail of an accessible recipe")
            .Produces<RecipeResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut(RecipesApiRoutes.RecipeById, UpdateRecipeAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateRecipe")
            .WithSummary("Replaces an accessible recipe and its ingredient and step collections")
            .Produces<RecipeResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete(RecipesApiRoutes.RecipeById, DeleteRecipeAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteRecipe")
            .WithSummary("Deletes an accessible recipe")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

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
        menus.MapGet("", ListWeeklyMenusAsync)
            .WithName("ListWeeklyMenus")
            .WithSummary("Returns accessible weekly menus, optionally filtered by week")
            .Produces<IReadOnlyList<WeeklyMenuSummaryResponse>>();
        menus.MapPost("", CreateWeeklyMenuAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateWeeklyMenu")
            .WithSummary("Creates a weekly menu")
            .Produces<WeeklyMenuResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        menus.MapGet(RecipesApiRoutes.MenuById, GetWeeklyMenuAsync)
            .WithName("GetWeeklyMenu")
            .WithSummary("Returns an accessible weekly menu detail")
            .Produces<WeeklyMenuResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        menus.MapPut(RecipesApiRoutes.MenuById, UpdateWeeklyMenuAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateWeeklyMenu")
            .WithSummary("Replaces an accessible weekly menu")
            .Produces<WeeklyMenuResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        menus.MapDelete(RecipesApiRoutes.MenuById, DeleteWeeklyMenuAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteWeeklyMenu")
            .WithSummary("Deletes an accessible weekly menu")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

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

    private static async Task<IResult> ListRecipesAsync(
        [AsParameters] RecipeListQuery query,
        RecipesReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListRecipesAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetRecipeAsync(
        int recipeId,
        RecipesReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var recipe = await read.GetRecipeAsync(recipeId, userId, cancellationToken);
        if (recipe is null)
        {
            throw RecipesRecipeProblem.NotFound();
        }

        return TypedResults.Ok(recipe);
    }

    private static async Task<IResult> CreateRecipeAsync(
        CreateRecipeRequest request,
        RecipesRecipeWriteService write,
        RecipesReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int recipeId;
        try
        {
            recipeId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (RecipesValidationException exception)
        {
            throw RecipesRecipeProblem.From(exception);
        }

        var created = await read.GetRecipeAsync(recipeId, userId, cancellationToken);
        return TypedResults.Created($"/api/recipes/{recipeId}", created);
    }

    private static async Task<IResult> UpdateRecipeAsync(
        int recipeId,
        UpdateRecipeRequest request,
        RecipesRecipeWriteService write,
        RecipesReadService read,
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
            updated = await write.UpdateAsync(recipeId, request, userId, cancellationToken);
        }
        catch (RecipesValidationException exception)
        {
            throw RecipesRecipeProblem.From(exception);
        }

        if (!updated)
        {
            throw RecipesRecipeProblem.NotFound();
        }

        var recipe = await read.GetRecipeAsync(recipeId, userId, cancellationToken);
        return TypedResults.Ok(recipe);
    }

    private static async Task<IResult> DeleteRecipeAsync(
        int recipeId,
        RecipesRecipeWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(recipeId, userId, cancellationToken);
        if (!deleted)
        {
            throw RecipesRecipeProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListWeeklyMenusAsync(
        [AsParameters] WeeklyMenuListQuery query,
        WeeklyMenusReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var menus = await read.ListAsync(query.ToFilter(), userId, cancellationToken);
        return TypedResults.Ok(menus);
    }

    private static async Task<IResult> GetWeeklyMenuAsync(
        int menuId,
        WeeklyMenusReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var menu = await read.GetAsync(menuId, userId, cancellationToken);
        if (menu is null)
        {
            throw RecipesMenuProblem.NotFound();
        }

        return TypedResults.Ok(menu);
    }

    private static async Task<IResult> CreateWeeklyMenuAsync(
        CreateWeeklyMenuRequest request,
        WeeklyMenusWriteService write,
        WeeklyMenusReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int menuId;
        try
        {
            menuId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (RecipesValidationException exception)
        {
            throw RecipesMenuProblem.From(exception);
        }

        var created = await read.GetAsync(menuId, userId, cancellationToken);
        return TypedResults.Created($"/api/recipes/menus/{menuId}", created);
    }

    private static async Task<IResult> UpdateWeeklyMenuAsync(
        int menuId,
        UpdateWeeklyMenuRequest request,
        WeeklyMenusWriteService write,
        WeeklyMenusReadService read,
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
            updated = await write.UpdateAsync(menuId, request, userId, cancellationToken);
        }
        catch (RecipesValidationException exception)
        {
            throw RecipesMenuProblem.From(exception);
        }

        if (!updated)
        {
            throw RecipesMenuProblem.NotFound();
        }

        var menu = await read.GetAsync(menuId, userId, cancellationToken);
        return TypedResults.Ok(menu);
    }

    private static async Task<IResult> DeleteWeeklyMenuAsync(
        int menuId,
        WeeklyMenusWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(menuId, userId, cancellationToken);
        if (!deleted)
        {
            throw RecipesMenuProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

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

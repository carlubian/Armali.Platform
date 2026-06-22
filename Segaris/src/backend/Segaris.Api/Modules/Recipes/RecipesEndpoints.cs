using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Recipes;

/// <summary>
/// Maps the Recipes HTTP surface frozen in Wave 0. Later waves replace these
/// placeholders with recipe, ingredient, menu, category, and attachment behaviour.
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

        group.MapGet("/categories", NotImplemented).WithName("ListRecipeCategories");

        return endpoints;
    }

    private static IResult NotImplemented() =>
        Results.StatusCode(StatusCodes.Status501NotImplemented);
}

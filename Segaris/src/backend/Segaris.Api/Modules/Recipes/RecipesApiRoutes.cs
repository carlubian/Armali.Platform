namespace Segaris.Api.Modules.Recipes;

/// <summary>Frozen route shapes for the Recipes HTTP surface.</summary>
internal static class RecipesApiRoutes
{
    public const string Tag = "Recipes";

    // The recipe collection lives at the module group root (/api/recipes).
    public const string Recipes = "recipes";
    public const string RecipeById = "/{recipeId:int}";
    public const string RecipeAttachments = "/{recipeId:int}/attachments";
    public const string RecipeAttachmentById = "/{recipeId:int}/attachments/{attachmentId}";
    public const string RecipePrimaryAttachment = "/{recipeId:int}/attachments/{attachmentId}/primary";

    public const string Menus = "recipes/menus";
    public const string MenuById = "/{menuId:int}";

    public const string Categories = "recipes/categories";
    public const string CategoryById = "/{categoryId:int}";
    public const string CategoryMove = "/{categoryId:int}/move";
    public const string CategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string CategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";
}

using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Recipes;

/// <summary>Stable machine-readable Recipes failures.</summary>
internal static class RecipesErrorCodes
{
    public static readonly ErrorCode RecipeNotFound = new("recipes.recipe.not_found");
    public static readonly ErrorCode RecipeValidation = new("recipes.recipe.validation");
    public static readonly ErrorCode RecipeVisibilityForbidden = new("recipes.recipe.visibility_forbidden");

    public static readonly ErrorCode IngredientItemNotAccessible = new("recipes.ingredient.item_not_accessible");
    public static readonly ErrorCode IngredientItemVisibilityForbidden = new("recipes.ingredient.item_visibility_forbidden");

    public static readonly ErrorCode MenuNotFound = new("recipes.menu.not_found");
    public static readonly ErrorCode MenuValidation = new("recipes.menu.validation");
    public static readonly ErrorCode MenuVisibilityForbidden = new("recipes.menu.visibility_forbidden");
    public static readonly ErrorCode MenuRecipeNotAccessible = new("recipes.menu.recipe_not_accessible");
    public static readonly ErrorCode MenuRecipeVisibilityForbidden = new("recipes.menu.recipe_visibility_forbidden");

    public static readonly ErrorCode UnknownCatalogReference = new("recipes.catalog.unknown_reference");

    public static readonly ErrorCode AttachmentNotFound = new("recipes.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("recipes.attachment.invalid");
    public static readonly ErrorCode AttachmentPrimaryInvalid = new("recipes.attachment.primary_invalid");

    public static readonly ErrorCode CategoryNotFound = new("recipes.category.not_found");
    public static readonly ErrorCode CategoryValidation = new("recipes.category.validation");
    public static readonly ErrorCode CategoryDuplicateName = new("recipes.category.duplicate_name");
    public static readonly ErrorCode CategoryRequiredNotEmpty = new("recipes.category.required_not_empty");
    public static readonly ErrorCode CategoryReferenced = new("recipes.category.referenced");
    public static readonly ErrorCode CategoryInvalidReplacement = new("recipes.category.invalid_replacement");
    public static readonly ErrorCode CategoryMigrationConflict = new("recipes.category.migration_conflict");
}

using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Recipes;

internal static class RecipesCategoryProblem
{
    public static ApiProblemException NotFound() =>
        new(StatusCodes.Status404NotFound, RecipesErrorCodes.CategoryNotFound, "Recipe category not found.");

    public static ApiProblemException Validation(string field, string message) =>
        new(StatusCodes.Status400BadRequest, RecipesErrorCodes.CategoryValidation, "Category validation failed.",
            errors: new Dictionary<string, string[]> { [field] = [message] });

    public static ApiProblemException DuplicateName() =>
        new(StatusCodes.Status409Conflict, RecipesErrorCodes.CategoryDuplicateName, "Category name already exists.");

    public static ApiProblemException RequiredNotEmpty() =>
        new(StatusCodes.Status409Conflict, RecipesErrorCodes.CategoryRequiredNotEmpty,
            "The recipe category catalog cannot be empty.");

    public static ApiProblemException Referenced() =>
        new(StatusCodes.Status409Conflict, RecipesErrorCodes.CategoryReferenced,
            "The category is referenced by at least one recipe.");

    public static ApiProblemException InvalidReplacement() =>
        new(StatusCodes.Status400BadRequest, RecipesErrorCodes.CategoryInvalidReplacement,
            "The replacement category is invalid.");

    public static ApiProblemException MigrationConflict() =>
        new(StatusCodes.Status409Conflict, RecipesErrorCodes.CategoryMigrationConflict,
            "The category migration conflicted with a concurrent change.");
}

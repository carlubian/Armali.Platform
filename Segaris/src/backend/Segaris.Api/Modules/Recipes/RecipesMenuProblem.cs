using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Recipes;

/// <summary>
/// Translates weekly menu domain failures into the frozen Recipes menu problem
/// codes. Missing and inaccessible menus share not-found semantics.
/// </summary>
internal static class RecipesMenuProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        RecipesErrorCodes.MenuNotFound,
        "The requested weekly menu was not found.");

    public static ApiProblemException From(RecipesValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            RecipesValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                RecipesErrorCodes.MenuVisibilityForbidden,
                exception.Message),
            RecipesValidationReason.MenuRecipeNotAccessible => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                RecipesErrorCodes.MenuRecipeNotAccessible,
                exception.Message),
            RecipesValidationReason.MenuRecipeVisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                RecipesErrorCodes.MenuRecipeVisibilityForbidden,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                RecipesErrorCodes.MenuValidation,
                exception.Message),
        };
    }
}

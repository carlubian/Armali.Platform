using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Recipes;

/// <summary>
/// Translates recipe domain failures into HTTP problem responses carrying the
/// frozen Recipes error-code values. Missing and inaccessible recipes share
/// not-found semantics so private data is not disclosed.
/// </summary>
internal static class RecipesRecipeProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        RecipesErrorCodes.RecipeNotFound,
        "The requested recipe was not found.");

    public static ApiProblemException From(RecipesValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            RecipesValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                RecipesErrorCodes.UnknownCatalogReference,
                exception.Message),
            RecipesValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                RecipesErrorCodes.RecipeVisibilityForbidden,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                RecipesErrorCodes.RecipeValidation,
                exception.Message),
        };
    }
}

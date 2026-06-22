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

    public static ApiProblemException AttachmentNotFound() => new(
        StatusCodes.Status404NotFound,
        RecipesErrorCodes.AttachmentNotFound,
        "The requested recipe attachment was not found.");

    public static ApiProblemException AttachmentInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        RecipesErrorCodes.AttachmentInvalid,
        "The attachment is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });

    public static ApiProblemException PrimaryNotImage() => new(
        StatusCodes.Status400BadRequest,
        RecipesErrorCodes.AttachmentPrimaryInvalid,
        "Only image attachments can be marked as the primary image.");

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
            RecipesValidationReason.IngredientItemNotAccessible => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                RecipesErrorCodes.IngredientItemNotAccessible,
                exception.Message),
            RecipesValidationReason.IngredientItemVisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                RecipesErrorCodes.IngredientItemVisibilityForbidden,
                exception.Message),
            RecipesValidationReason.MenuRecipeVisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                RecipesErrorCodes.MenuRecipeVisibilityForbidden,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                RecipesErrorCodes.RecipeValidation,
                exception.Message),
        };
    }
}

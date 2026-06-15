using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Capex;

internal static class CapexCategoryProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, CapexErrorCodes.CategoryNotFound, "Capex category not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, CapexErrorCodes.CategoryValidation, "Category validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, CapexErrorCodes.CategoryDuplicateName, "Category name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, CapexErrorCodes.CategoryRequiredNotEmpty, "The category catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, CapexErrorCodes.CategoryReferenced, "The category is referenced.");
}

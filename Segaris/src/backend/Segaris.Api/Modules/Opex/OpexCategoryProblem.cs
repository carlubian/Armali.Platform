using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Opex;

internal static class OpexCategoryProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, OpexErrorCodes.CategoryNotFound, "Opex category not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, OpexErrorCodes.CategoryValidation, "Category validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, OpexErrorCodes.CategoryDuplicateName, "Category name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, OpexErrorCodes.CategoryRequiredNotEmpty, "The category catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, OpexErrorCodes.CategoryReferenced, "The category is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, OpexErrorCodes.CategoryInvalidReplacement, "The replacement category is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, OpexErrorCodes.CategoryMigrationConflict, "The category migration conflicted with a concurrent change.");
}

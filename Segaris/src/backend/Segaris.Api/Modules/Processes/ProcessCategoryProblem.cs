using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Processes;

/// <summary>
/// Translates Processes category catalogue failures into HTTP problem responses carrying
/// the frozen <see cref="ProcessesErrorCodes"/> values. Because a category is required on
/// every process, a referenced value may only be replaced, never cleared or directly
/// deleted while referenced.
/// </summary>
internal static class ProcessCategoryProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, ProcessesErrorCodes.CategoryNotFound, "Process category not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, ProcessesErrorCodes.CategoryValidation, "Process category validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, ProcessesErrorCodes.CategoryDuplicateName, "Process category name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, ProcessesErrorCodes.CategoryRequiredNotEmpty, "The process category catalogue cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, ProcessesErrorCodes.CategoryReferenced, "The process category is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, ProcessesErrorCodes.CategoryInvalidReplacement, "The replacement process category is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, ProcessesErrorCodes.CategoryMigrationConflict, "The process category migration conflicted with a concurrent change.");
}

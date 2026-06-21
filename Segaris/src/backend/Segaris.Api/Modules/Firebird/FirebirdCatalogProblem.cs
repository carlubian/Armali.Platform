using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Firebird;

internal static class PersonCategoryProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, FirebirdErrorCodes.CategoryNotFound, "Person category not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, FirebirdErrorCodes.CategoryValidation, "Person category validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, FirebirdErrorCodes.CategoryDuplicateName, "Person category name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, FirebirdErrorCodes.CategoryRequiredNotEmpty, "The person category catalogue cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, FirebirdErrorCodes.CategoryReferenced, "The person category is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, FirebirdErrorCodes.CategoryInvalidReplacement, "The replacement person category is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, FirebirdErrorCodes.CategoryMigrationConflict, "The person category migration conflicted with a concurrent change.");
}

internal static class UsernamePlatformProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, FirebirdErrorCodes.PlatformNotFound, "Username platform not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, FirebirdErrorCodes.PlatformValidation, "Username platform validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, FirebirdErrorCodes.PlatformDuplicateName, "Username platform name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, FirebirdErrorCodes.PlatformRequiredNotEmpty, "The username platform catalogue cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, FirebirdErrorCodes.PlatformReferenced, "The username platform is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, FirebirdErrorCodes.PlatformInvalidReplacement, "The replacement username platform is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, FirebirdErrorCodes.PlatformMigrationConflict, "The username platform migration conflicted with a concurrent change.");
}


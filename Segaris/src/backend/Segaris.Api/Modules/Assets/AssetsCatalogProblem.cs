using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Assets;

internal static class AssetCategoryProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, AssetsErrorCodes.CategoryNotFound, "Asset category not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, AssetsErrorCodes.CategoryValidation, "Category validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, AssetsErrorCodes.CategoryDuplicateName, "Category name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, AssetsErrorCodes.CategoryRequiredNotEmpty, "The category catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, AssetsErrorCodes.CategoryReferenced, "The category is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, AssetsErrorCodes.CategoryInvalidReplacement, "The replacement category is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, AssetsErrorCodes.CategoryMigrationConflict, "The category migration conflicted with a concurrent change.");
}

internal static class AssetLocationProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, AssetsErrorCodes.LocationNotFound, "Asset location not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, AssetsErrorCodes.LocationValidation, "Location validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, AssetsErrorCodes.LocationDuplicateName, "Location name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, AssetsErrorCodes.LocationRequiredNotEmpty, "The location catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, AssetsErrorCodes.LocationReferenced, "The location is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, AssetsErrorCodes.LocationInvalidReplacement, "The replacement location is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, AssetsErrorCodes.LocationMigrationConflict, "The location migration conflicted with a concurrent change.");
}

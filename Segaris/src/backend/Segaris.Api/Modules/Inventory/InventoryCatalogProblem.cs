using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Inventory;

internal static class InventoryCategoryProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, InventoryErrorCodes.CategoryNotFound, "Inventory category not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, InventoryErrorCodes.CategoryValidation, "Category validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, InventoryErrorCodes.CategoryDuplicateName, "Category name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, InventoryErrorCodes.CategoryRequiredNotEmpty, "The category catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, InventoryErrorCodes.CategoryReferenced, "The category is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, InventoryErrorCodes.CategoryInvalidReplacement, "The replacement category is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, InventoryErrorCodes.CategoryMigrationConflict, "The category migration conflicted with a concurrent change.");
}

internal static class InventoryLocationProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, InventoryErrorCodes.LocationNotFound, "Inventory location not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, InventoryErrorCodes.LocationValidation, "Location validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, InventoryErrorCodes.LocationDuplicateName, "Location name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, InventoryErrorCodes.LocationRequiredNotEmpty, "The location catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, InventoryErrorCodes.LocationReferenced, "The location is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, InventoryErrorCodes.LocationInvalidReplacement, "The replacement location is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, InventoryErrorCodes.LocationMigrationConflict, "The location migration conflicted with a concurrent change.");
}

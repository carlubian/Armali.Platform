using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Clothes;

internal static class ClothesCategoryProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, ClothesErrorCodes.CategoryNotFound, "Clothing category not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, ClothesErrorCodes.CategoryValidation, "Category validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, ClothesErrorCodes.CategoryDuplicateName, "Category name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, ClothesErrorCodes.CategoryRequiredNotEmpty, "The category catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, ClothesErrorCodes.CategoryReferenced, "The category is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, ClothesErrorCodes.CategoryInvalidReplacement, "The replacement category is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, ClothesErrorCodes.CategoryMigrationConflict, "The category migration conflicted with a concurrent change.");
}

internal static class ClothesColorProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, ClothesErrorCodes.ColorNotFound, "Clothing colour not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, ClothesErrorCodes.ColorValidation, "Colour validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, ClothesErrorCodes.ColorDuplicateName, "Colour name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, ClothesErrorCodes.ColorRequiredNotEmpty, "The colour catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, ClothesErrorCodes.ColorReferenced, "The colour is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, ClothesErrorCodes.ColorInvalidReplacement, "The replacement colour is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, ClothesErrorCodes.ColorMigrationConflict, "The colour migration conflicted with a concurrent change.");
}

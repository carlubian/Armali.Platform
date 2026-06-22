using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Destinations;

internal static class DestinationCategoryProblem
{
    public static ApiProblemException NotFound() =>
        new(StatusCodes.Status404NotFound, DestinationsErrorCodes.CategoryNotFound, "Destination category not found.");

    public static ApiProblemException Validation(string field, string message) =>
        new(StatusCodes.Status400BadRequest, DestinationsErrorCodes.CategoryValidation, "Destination category validation failed.",
            errors: new Dictionary<string, string[]> { [field] = [message] });

    public static ApiProblemException DuplicateName() =>
        new(StatusCodes.Status409Conflict, DestinationsErrorCodes.CategoryDuplicateName, "Destination category name already exists.");

    public static ApiProblemException RequiredNotEmpty() =>
        new(StatusCodes.Status409Conflict, DestinationsErrorCodes.CategoryRequiredNotEmpty,
            "The destination category catalogue cannot be empty.");

    public static ApiProblemException Referenced() =>
        new(StatusCodes.Status409Conflict, DestinationsErrorCodes.CategoryReferenced,
            "The category is referenced by at least one destination.");

    public static ApiProblemException InvalidReplacement() =>
        new(StatusCodes.Status400BadRequest, DestinationsErrorCodes.CategoryInvalidReplacement,
            "The replacement destination category is invalid.");

    public static ApiProblemException MigrationConflict() =>
        new(StatusCodes.Status409Conflict, DestinationsErrorCodes.CategoryMigrationConflict,
            "The destination category migration conflicted with a concurrent change.");
}

internal static class PlaceCategoryProblem
{
    public static ApiProblemException NotFound() =>
        new(StatusCodes.Status404NotFound, DestinationsErrorCodes.PlaceCategoryNotFound, "Place category not found.");

    public static ApiProblemException Validation(string field, string message) =>
        new(StatusCodes.Status400BadRequest, DestinationsErrorCodes.PlaceCategoryValidation, "Place category validation failed.",
            errors: new Dictionary<string, string[]> { [field] = [message] });

    public static ApiProblemException DuplicateName() =>
        new(StatusCodes.Status409Conflict, DestinationsErrorCodes.PlaceCategoryDuplicateName, "Place category name already exists.");

    public static ApiProblemException RequiredNotEmpty() =>
        new(StatusCodes.Status409Conflict, DestinationsErrorCodes.PlaceCategoryRequiredNotEmpty,
            "The place category catalogue cannot be empty.");

    public static ApiProblemException Referenced() =>
        new(StatusCodes.Status409Conflict, DestinationsErrorCodes.PlaceCategoryReferenced,
            "The category is referenced by at least one place.");

    public static ApiProblemException InvalidReplacement() =>
        new(StatusCodes.Status400BadRequest, DestinationsErrorCodes.PlaceCategoryInvalidReplacement,
            "The replacement place category is invalid.");

    public static ApiProblemException MigrationConflict() =>
        new(StatusCodes.Status409Conflict, DestinationsErrorCodes.PlaceCategoryMigrationConflict,
            "The place category migration conflicted with a concurrent change.");
}

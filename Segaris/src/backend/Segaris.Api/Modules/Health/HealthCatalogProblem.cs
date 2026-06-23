using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Health;

internal static class DiseaseCategoryProblem
{
    public static ApiProblemException NotFound() =>
        new(StatusCodes.Status404NotFound, HealthErrorCodes.DiseaseCategoryNotFound, "Disease category not found.");

    public static ApiProblemException Validation(string field, string message) =>
        new(StatusCodes.Status400BadRequest, HealthErrorCodes.DiseaseCategoryValidation, "Disease category validation failed.",
            errors: new Dictionary<string, string[]> { [field] = [message] });

    public static ApiProblemException DuplicateName() =>
        new(StatusCodes.Status409Conflict, HealthErrorCodes.DiseaseCategoryDuplicateName, "Disease category name already exists.");

    public static ApiProblemException RequiredNotEmpty() =>
        new(StatusCodes.Status409Conflict, HealthErrorCodes.DiseaseCategoryRequiredNotEmpty,
            "The disease category catalogue cannot be empty.");

    public static ApiProblemException Referenced() =>
        new(StatusCodes.Status409Conflict, HealthErrorCodes.DiseaseCategoryReferenced,
            "The category is referenced by at least one disease.");

    public static ApiProblemException InvalidReplacement() =>
        new(StatusCodes.Status400BadRequest, HealthErrorCodes.DiseaseCategoryInvalidReplacement,
            "The replacement disease category is invalid.");

    public static ApiProblemException MigrationConflict() =>
        new(StatusCodes.Status409Conflict, HealthErrorCodes.DiseaseCategoryMigrationConflict,
            "The disease category migration conflicted with a concurrent change.");
}

internal static class MedicineCategoryProblem
{
    public static ApiProblemException NotFound() =>
        new(StatusCodes.Status404NotFound, HealthErrorCodes.MedicineCategoryNotFound, "Medicine category not found.");

    public static ApiProblemException Validation(string field, string message) =>
        new(StatusCodes.Status400BadRequest, HealthErrorCodes.MedicineCategoryValidation, "Medicine category validation failed.",
            errors: new Dictionary<string, string[]> { [field] = [message] });

    public static ApiProblemException DuplicateName() =>
        new(StatusCodes.Status409Conflict, HealthErrorCodes.MedicineCategoryDuplicateName, "Medicine category name already exists.");

    public static ApiProblemException RequiredNotEmpty() =>
        new(StatusCodes.Status409Conflict, HealthErrorCodes.MedicineCategoryRequiredNotEmpty,
            "The medicine category catalogue cannot be empty.");

    public static ApiProblemException Referenced() =>
        new(StatusCodes.Status409Conflict, HealthErrorCodes.MedicineCategoryReferenced,
            "The category is referenced by at least one medicine.");

    public static ApiProblemException InvalidReplacement() =>
        new(StatusCodes.Status400BadRequest, HealthErrorCodes.MedicineCategoryInvalidReplacement,
            "The replacement medicine category is invalid.");

    public static ApiProblemException MigrationConflict() =>
        new(StatusCodes.Status409Conflict, HealthErrorCodes.MedicineCategoryMigrationConflict,
            "The medicine category migration conflicted with a concurrent change.");
}

using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Travel;

internal static class TravelTripTypeProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, TravelErrorCodes.TripTypeNotFound, "Trip type not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, TravelErrorCodes.TripTypeValidation, "Trip type validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, TravelErrorCodes.TripTypeDuplicateName, "Trip type name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, TravelErrorCodes.TripTypeRequiredNotEmpty, "The trip type catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, TravelErrorCodes.TripTypeReferenced, "The trip type is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, TravelErrorCodes.TripTypeInvalidReplacement, "The replacement trip type is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, TravelErrorCodes.TripTypeMigrationConflict, "The trip type migration conflicted with a concurrent change.");
}

internal static class TravelExpenseCategoryProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, TravelErrorCodes.ExpenseCategoryNotFound, "Expense category not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, TravelErrorCodes.ExpenseCategoryValidation, "Expense category validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, TravelErrorCodes.ExpenseCategoryDuplicateName, "Expense category name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, TravelErrorCodes.ExpenseCategoryRequiredNotEmpty, "The expense category catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, TravelErrorCodes.ExpenseCategoryReferenced, "The expense category is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, TravelErrorCodes.ExpenseCategoryInvalidReplacement, "The replacement expense category is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, TravelErrorCodes.ExpenseCategoryMigrationConflict, "The expense category migration conflicted with a concurrent change.");
}

using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Maintenance;

/// <summary>
/// Translates Maintenance type catalogue failures into HTTP problem responses carrying
/// the frozen <see cref="MaintenanceErrorCodes"/> values. Because a type is required on
/// every task, a referenced value may only be replaced, never cleared or directly
/// deleted while referenced.
/// </summary>
internal static class MaintenanceTypeProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, MaintenanceErrorCodes.TypeNotFound, "Maintenance type not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, MaintenanceErrorCodes.TypeValidation, "Maintenance type validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, MaintenanceErrorCodes.TypeDuplicateName, "Maintenance type name already exists.");
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, MaintenanceErrorCodes.TypeRequiredNotEmpty, "The maintenance type catalogue cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, MaintenanceErrorCodes.TypeReferenced, "The maintenance type is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, MaintenanceErrorCodes.TypeInvalidReplacement, "The replacement maintenance type is invalid.");
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, MaintenanceErrorCodes.TypeMigrationConflict, "The maintenance type migration conflicted with a concurrent change.");
}

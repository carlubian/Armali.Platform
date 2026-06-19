using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Maintenance;

/// <summary>Translates Maintenance task failures into stable HTTP problem responses.</summary>
internal static class MaintenanceTaskProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        MaintenanceErrorCodes.TaskNotFound,
        "Maintenance task not found.");

    public static ApiProblemException From(MaintenanceValidationException exception) =>
        exception.Reason switch
        {
            MaintenanceValidationReason.UnknownType => new(
                StatusCodes.Status400BadRequest,
                MaintenanceErrorCodes.UnknownType,
                "Maintenance task validation failed.",
                errors: Errors("maintenanceTypeId", exception.Message)),
            MaintenanceValidationReason.VisibilityForbidden => new(
                StatusCodes.Status403Forbidden,
                MaintenanceErrorCodes.TaskVisibilityForbidden,
                "Maintenance task visibility change is forbidden.",
                errors: Errors("visibility", exception.Message)),
            MaintenanceValidationReason.AssetReference => new(
                StatusCodes.Status400BadRequest,
                MaintenanceErrorCodes.AssetReferenceInvalid,
                "Maintenance task asset reference is invalid.",
                errors: Errors("assetId", exception.Message)),
            MaintenanceValidationReason.AssetVisibilityForbidden => new(
                StatusCodes.Status400BadRequest,
                MaintenanceErrorCodes.AssetVisibilityForbidden,
                "Maintenance task asset visibility is incompatible.",
                errors: Errors("assetId", exception.Message)),
            _ => Validation(exception.Message),
        };

    public static ApiProblemException Validation(string message) => new(
        StatusCodes.Status400BadRequest,
        MaintenanceErrorCodes.TaskValidation,
        "Maintenance task validation failed.",
        errors: Errors("task", message));

    private static Dictionary<string, string[]> Errors(string field, string message) =>
        new(StringComparer.Ordinal)
        {
            [field] = [message],
        };
}

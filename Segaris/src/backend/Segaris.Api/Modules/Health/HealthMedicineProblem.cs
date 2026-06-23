using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Health;

/// <summary>
/// Translates Health medicine domain failures into HTTP problem responses carrying
/// the frozen Health error codes. Missing and inaccessible medicines share not-found
/// semantics so private data is not disclosed.
/// </summary>
internal static class HealthMedicineProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        HealthErrorCodes.MedicineNotFound,
        "The requested Health medicine was not found.");

    public static ApiProblemException From(HealthValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            HealthValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                HealthErrorCodes.UnknownCatalogReference,
                exception.Message),
            HealthValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                HealthErrorCodes.MedicineVisibilityForbidden,
                exception.Message),
            HealthValidationReason.AssociationPublishBlocked => HealthAssociationProblem.From(exception),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                HealthErrorCodes.MedicineValidation,
                exception.Message),
        };
    }
}

using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Health;

/// <summary>
/// Translates Health disease domain failures into HTTP problem responses carrying
/// the frozen Health error codes. Missing and inaccessible diseases share not-found
/// semantics so private data is not disclosed.
/// </summary>
internal static class HealthDiseaseProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        HealthErrorCodes.DiseaseNotFound,
        "The requested Health disease was not found.");

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
                HealthErrorCodes.DiseaseVisibilityForbidden,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                HealthErrorCodes.DiseaseValidation,
                exception.Message),
        };
    }
}

using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Health;

/// <summary>Translates Health association failures into stable HTTP problems.</summary>
internal static class HealthAssociationProblem
{
    public static ApiProblemException From(HealthValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            HealthValidationReason.AssociationNotAccessible => new ApiProblemException(
                StatusCodes.Status409Conflict,
                HealthErrorCodes.AssociationNotAccessible,
                exception.Message),
            HealthValidationReason.AssociationVisibilityForbidden => new ApiProblemException(
                StatusCodes.Status409Conflict,
                HealthErrorCodes.AssociationVisibilityForbidden,
                exception.Message),
            HealthValidationReason.AssociationPublishBlocked => new ApiProblemException(
                StatusCodes.Status409Conflict,
                HealthErrorCodes.AssociationPublishBlocked,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                HealthErrorCodes.DiseaseValidation,
                exception.Message),
        };
    }
}

using Microsoft.AspNetCore.Identity;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Identity;

/// <summary>
/// Translates a failed <see cref="IdentityResult"/> into the platform problem
/// contract with a stable error code and field-keyed validation errors.
/// </summary>
internal static class IdentityProblem
{
    public static ApiProblemException FromResult(IdentityResult result, string field)
    {
        var descriptions = result.Errors
            .Select(error => error.Description)
            .ToArray();

        var isConflict = result.Errors.Any(error =>
            error.Code.StartsWith("Duplicate", StringComparison.Ordinal));

        if (isConflict)
        {
            return new ApiProblemException(
                StatusCodes.Status409Conflict,
                ApiErrorCodes.Conflict,
                "The request conflicts with the current state.",
                detail: string.Join(" ", descriptions));
        }

        return new ApiProblemException(
            StatusCodes.Status400BadRequest,
            ApiErrorCodes.BadRequest,
            "One or more request values are invalid.",
            errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [field] = descriptions,
            });
    }
}

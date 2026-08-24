using Blackwing.Api.Platform.Api;
using Microsoft.AspNetCore.Identity;

namespace Blackwing.Api.Modules.Identity;

/// <summary>
/// Translates a failed <see cref="IdentityResult"/> into the platform problem contract with a
/// stable error code and field-keyed validation errors.
/// </summary>
internal static class IdentityProblem
{
    public static ApiProblemException FromResult(IdentityResult result, string field)
    {
        ArgumentNullException.ThrowIfNull(result);

        var descriptions = result.Errors
            .Select(error => error.Description)
            .ToArray();

        // A duplicate is a state conflict, not a malformed request: reporting it as 400 would
        // make a client retry the same body forever.
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

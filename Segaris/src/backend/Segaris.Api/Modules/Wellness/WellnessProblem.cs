using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Wellness;

/// <summary>
/// Maps Wellness domain failures to the stable <see cref="ApiProblemException"/>
/// shape. The task catalogue is a reduced create/list/delete surface: deletion is
/// impact-free because days hold task snapshots, so there is no referenced or
/// replacement problem, only not-found and validation.
/// </summary>
internal static class WellnessProblem
{
    public static ApiProblemException TaskNotFound() =>
        new(StatusCodes.Status404NotFound, WellnessErrorCodes.TaskNotFound, "Wellness task not found.");

    public static ApiProblemException TaskValidation(string field, string message) =>
        new(
            StatusCodes.Status400BadRequest,
            WellnessErrorCodes.TaskValidation,
            "Wellness task validation failed.",
            errors: new Dictionary<string, string[]> { [field] = [message] });
}

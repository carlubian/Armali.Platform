using Segaris.Api.Modules.Games.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Games;

/// <summary>
/// Maps playthrough-scoped section and goal failures while keeping inaccessible
/// parent records indistinguishable from absent records.
/// </summary>
internal static class SectionGoalProblem
{
    public static ApiProblemException SectionNotFound() => new(
        StatusCodes.Status404NotFound,
        GamesErrorCodes.SectionNotFound,
        "Section not found.");

    public static ApiProblemException GoalNotFound() => new(
        StatusCodes.Status404NotFound,
        GamesErrorCodes.GoalNotFound,
        "Goal not found.");

    public static ApiProblemException SectionDuplicateName() => new(
        StatusCodes.Status409Conflict,
        GamesErrorCodes.SectionDuplicateName,
        "Section name already exists.");

    public static ApiProblemException SectionInvalidOrder() => new(
        StatusCodes.Status400BadRequest,
        GamesErrorCodes.SectionInvalidOrder,
        "Section order is invalid.");

    public static ApiProblemException SectionValidation(GamesValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ApiProblemException(
            StatusCodes.Status400BadRequest,
            GamesErrorCodes.SectionValidation,
            "Section validation failed.",
            errors: FieldErrors(exception, "name"));
    }

    public static ApiProblemException GoalValidation(GamesValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ApiProblemException(
            StatusCodes.Status400BadRequest,
            GamesErrorCodes.GoalValidation,
            "Goal validation failed.",
            errors: FieldErrors(exception, "text"));
    }

    private static Dictionary<string, string[]> FieldErrors(GamesValidationException exception, string fallbackField) =>
        new(StringComparer.Ordinal)
        {
            [exception.Field ?? fallbackField] = [exception.Message],
        };
}

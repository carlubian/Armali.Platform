using Segaris.Api.Modules.Games.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Games;

/// <summary>
/// Maps playthrough validation and authorization failures onto the frozen Games
/// error codes. Inaccessible playthroughs share the platform not-found behaviour so
/// private records are never disclosed.
/// </summary>
internal static class PlaythroughProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        GamesErrorCodes.PlaythroughNotFound,
        "Playthrough not found.");

    public static ApiProblemException From(GamesValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            GamesValidationReason.UnknownGame => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                GamesErrorCodes.UnknownGameReference,
                exception.Message,
                errors: FieldErrors(exception, "gameId")),
            GamesValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                GamesErrorCodes.PlaythroughVisibilityForbidden,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                GamesErrorCodes.PlaythroughValidation,
                "Playthrough validation failed.",
                errors: FieldErrors(exception, "name")),
        };
    }

    private static Dictionary<string, string[]> FieldErrors(GamesValidationException exception, string fallbackField) =>
        new(StringComparer.Ordinal)
        {
            [exception.Field ?? fallbackField] = [exception.Message],
        };
}

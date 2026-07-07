using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Games;

internal static class GameProblem
{
    public static ApiProblemException NotFound() =>
        new(StatusCodes.Status404NotFound, GamesErrorCodes.GameNotFound, "Game not found.");

    public static ApiProblemException Validation(string field, string message) =>
        new(
            StatusCodes.Status400BadRequest,
            GamesErrorCodes.GameValidation,
            "Game validation failed.",
            errors: new Dictionary<string, string[]> { [field] = [message] });

    public static ApiProblemException DuplicateName() =>
        new(StatusCodes.Status409Conflict, GamesErrorCodes.GameDuplicateName, "Game name already exists.");

    public static ApiProblemException Referenced() =>
        new(StatusCodes.Status409Conflict, GamesErrorCodes.GameReferenced, "The game is referenced.");

    public static ApiProblemException InvalidReplacement() =>
        new(StatusCodes.Status400BadRequest, GamesErrorCodes.GameInvalidReplacement, "The replacement game is invalid.");

    public static ApiProblemException MigrationConflict() =>
        new(StatusCodes.Status409Conflict, GamesErrorCodes.GameMigrationConflict, "The game migration conflicted with a concurrent change.");
}

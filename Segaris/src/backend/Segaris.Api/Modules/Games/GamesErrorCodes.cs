using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Games;

/// <summary>
/// Stable, Games-specific <see cref="ErrorCode"/> values returned through
/// <c>ApiProblemException</c>. Generic transport failures continue to use the
/// platform <c>ApiErrorCodes</c>; these codes name the Games domain failures that
/// clients and tests can rely on across Waves.
/// </summary>
internal static class GamesErrorCodes
{
    // Game catalogue. The catalogue may be empty, so there is no required-not-empty
    // code; a referenced game may only be replaced, never cleared or force-deleted.
    /// <summary>The addressed catalogue game does not exist.</summary>
    public static readonly ErrorCode GameNotFound = new("games.game.not_found");

    /// <summary>The game request failed validation; may carry field errors.</summary>
    public static readonly ErrorCode GameValidation = new("games.game.validation");

    /// <summary>Another game already uses the name (case-insensitive).</summary>
    public static readonly ErrorCode GameDuplicateName = new("games.game.duplicate_name");

    /// <summary>A direct delete was attempted on a game still referenced by playthroughs.</summary>
    public static readonly ErrorCode GameReferenced = new("games.game.referenced");

    /// <summary>The requested replacement game is missing, equal to the source, or invalid.</summary>
    public static readonly ErrorCode GameInvalidReplacement = new("games.game.invalid_replacement");

    /// <summary>A concurrent change invalidated the source, replacement, or references.</summary>
    public static readonly ErrorCode GameMigrationConflict = new("games.game.migration_conflict");

    // Playthrough.
    /// <summary>The playthrough is absent or hidden from the current user.</summary>
    public static readonly ErrorCode PlaythroughNotFound = new("games.playthrough.not_found");

    /// <summary>The playthrough payload failed validation; carries field errors.</summary>
    public static readonly ErrorCode PlaythroughValidation = new("games.playthrough.validation");

    /// <summary>The referenced game does not exist.</summary>
    public static readonly ErrorCode UnknownGameReference = new("games.playthrough.unknown_game");

    /// <summary>Only the creator may change a playthrough's visibility in either direction.</summary>
    public static readonly ErrorCode PlaythroughVisibilityForbidden = new("games.playthrough.visibility_forbidden");

    // Section.
    /// <summary>The section is absent or not owned by the addressed playthrough.</summary>
    public static readonly ErrorCode SectionNotFound = new("games.section.not_found");

    /// <summary>The section payload failed validation; may carry field errors.</summary>
    public static readonly ErrorCode SectionValidation = new("games.section.validation");

    /// <summary>Another section in the same playthrough already uses the name (case-insensitive).</summary>
    public static readonly ErrorCode SectionDuplicateName = new("games.section.duplicate_name");

    /// <summary>The section reorder payload does not match the playthrough's sections.</summary>
    public static readonly ErrorCode SectionInvalidOrder = new("games.section.invalid_order");

    // Goal.
    /// <summary>The goal is absent or not owned by the addressed section.</summary>
    public static readonly ErrorCode GoalNotFound = new("games.goal.not_found");

    /// <summary>The goal payload failed validation; may carry field errors.</summary>
    public static readonly ErrorCode GoalValidation = new("games.goal.validation");
}

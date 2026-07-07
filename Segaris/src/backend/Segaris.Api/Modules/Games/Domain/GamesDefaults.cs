using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Games.Domain;

/// <summary>Frozen Games defaults and validation bounds that are not catalogues.</summary>
internal static class GamesDefaults
{
    /// <summary>Maximum persisted length of a game, playthrough, or section name.</summary>
    public const int NameMaximumLength = 200;

    /// <summary>Maximum persisted length of a single goal's text.</summary>
    public const int GoalTextMaximumLength = 500;

    /// <summary>Maximum persisted length of a single normalized tag.</summary>
    public const int TagMaximumLength = 100;

    /// <summary>Inclusive lower bound of a playthrough's start month.</summary>
    public const int MinimumStartMonth = 1;

    /// <summary>Inclusive upper bound of a playthrough's start month.</summary>
    public const int MaximumStartMonth = 12;

    /// <summary>Inclusive lower bound of a playthrough's start year.</summary>
    public const int MinimumStartYear = 1;

    /// <summary>Inclusive upper bound of a playthrough's start year.</summary>
    public const int MaximumStartYear = 9999;

    /// <summary>Default status assigned to a new playthrough.</summary>
    public const PlaythroughStatus Status = PlaythroughStatus.Planning;

    /// <summary>Default completion flag assigned to a new goal.</summary>
    public const bool GoalCompleted = false;

    /// <summary>Default visibility assigned to a new playthrough.</summary>
    public static readonly RecordVisibility Visibility = RecordVisibility.Public;
}

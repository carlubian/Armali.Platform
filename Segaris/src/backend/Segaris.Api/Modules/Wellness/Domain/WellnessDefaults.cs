namespace Segaris.Api.Modules.Wellness.Domain;

/// <summary>
/// Frozen Wellness defaults and validation bounds that are not catalogues. The
/// daily task count is fixed for the initial release; the per-day score is the
/// integer percentage of the day's tasks that are completed.
/// </summary>
internal static class WellnessDefaults
{
    /// <summary>Maximum persisted length of a catalogue task name.</summary>
    public const int TaskNameMaximumLength = 200;

    /// <summary>Number of tasks selected for a generated day when the catalogue allows it.</summary>
    public const int DailyTaskCount = 6;

    /// <summary>Inclusive lower bound of a persisted day score.</summary>
    public const int MinimumScore = 0;

    /// <summary>Inclusive upper bound of a persisted day score.</summary>
    public const int MaximumScore = 100;

    /// <summary>Default completion flag assigned to a newly generated day task.</summary>
    public const bool TaskCompleted = false;
}

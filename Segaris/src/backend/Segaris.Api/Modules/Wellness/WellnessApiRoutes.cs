namespace Segaris.Api.Modules.Wellness;

/// <summary>
/// Frozen route shapes for the Wellness HTTP surface. The prefix is relative to
/// <c>/api</c> as required by <c>MapSegarisApiGroup</c>; the templates document the
/// per-route patterns. Today, day-task, and day records are always scoped to the
/// current user. The task catalogue follows the module-owned <c>{owner}/{catalog}</c>
/// convention surfaced through Configuration and is a reduced create/list/delete
/// surface: order is creation order and is not user-editable, and deletion is
/// impact-free because days hold task snapshots, so there is no update, move,
/// deletion-impact, or replace route.
/// </summary>
internal static class WellnessApiRoutes
{
    public const string Tag = "Wellness";
    public const string Wellness = "wellness";

    // Per-user daily surface.
    /// <summary>The current household day's selected tasks and score, generated on first read.</summary>
    public const string Today = "wellness/today";

    /// <summary>Flips one day-task's completion and returns the recomputed day.</summary>
    public const string TodayTaskToggle = "wellness/today/tasks/{dayTaskId:int}/toggle";

    // Per-user day-range read consumed by the Mood weekly log.
    /// <summary>Per-day scores for existing days in an inclusive date range, current user only.</summary>
    public const string Days = "wellness/days";

    /// <summary>Inclusive lower bound for the days range query.</summary>
    public const string FromQuery = "from";

    /// <summary>Inclusive upper bound for the days range query.</summary>
    public const string ToQuery = "to";

    // Module-owned task catalogue surfaced through Configuration.
    public const string TaskCatalogue = "wellness/tasks";
    public const string TaskById = "/{taskId:int}";
}

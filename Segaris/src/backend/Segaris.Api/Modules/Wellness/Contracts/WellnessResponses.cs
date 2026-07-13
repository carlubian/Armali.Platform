namespace Segaris.Api.Modules.Wellness.Contracts;

/// <summary>A module-owned catalogue task surfaced through Configuration. Category is
/// projected as its stable enum name; SortOrder is creation order.</summary>
internal sealed record WellnessTaskResponse(int Id, string Name, string Category, int SortOrder);

/// <summary>
/// One selected task inside the current household day. The name and category are the
/// persisted snapshot copied when the day was generated, so they are independent of
/// the live catalogue. Position is the stable in-day order.
/// </summary>
internal sealed record WellnessDayTaskResponse(
    int Id,
    string Name,
    string Category,
    bool Completed,
    int Position);

/// <summary>
/// The current household day's selected tasks and score for the current user.
/// <see cref="Score"/> is the integer percentage of completed tasks (<c>0</c>-<c>100</c>)
/// and is <c>null</c> only for a day with no tasks (an empty catalogue). A visited day
/// with zero completed tasks reports <c>0</c>, not <c>null</c>.
/// </summary>
internal sealed record WellnessTodayResponse(
    DateOnly Date,
    int? Score,
    IReadOnlyList<WellnessDayTaskResponse> Tasks);

/// <summary>
/// Per-day score for one existing day in a range read. This is the projection the
/// Mood weekly log consumes. <see cref="Score"/> follows the same nullability as the
/// today surface: <c>null</c> only for a day with no tasks, otherwise <c>0</c>-<c>100</c>.
/// </summary>
internal sealed record WellnessDayScoreResponse(DateOnly Date, int? Score);

/// <summary>
/// Owner-only day-range payload. Only existing days appear; days without a Wellness
/// record are simply absent so the Mood chart renders no Wellness marker for them.
/// </summary>
internal sealed record WellnessDayListResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<WellnessDayScoreResponse> Days);

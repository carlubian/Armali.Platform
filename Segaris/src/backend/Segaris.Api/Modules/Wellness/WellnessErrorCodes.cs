using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Wellness;

/// <summary>
/// Stable, Wellness-specific <see cref="ErrorCode"/> values returned through
/// <c>ApiProblemException</c>. Generic transport failures continue to use the
/// platform <c>ApiErrorCodes</c>; these codes name the Wellness domain failures that
/// clients and tests can rely on across Waves.
/// </summary>
internal static class WellnessErrorCodes
{
    // Task catalogue. The catalogue may be empty, so there is no required-not-empty
    // code; deletion is impact-free because days hold task snapshots, so there is no
    // referenced or replacement code.
    /// <summary>The addressed catalogue task does not exist.</summary>
    public static readonly ErrorCode TaskNotFound = new("wellness.task.not_found");

    /// <summary>The task request failed validation; may carry field errors.</summary>
    public static readonly ErrorCode TaskValidation = new("wellness.task.validation");

    // Day and day-task. Day, day-task, and today records are always scoped to the
    // current user and another user's records are never disclosed, so an inaccessible
    // day-task is reported as not found.
    /// <summary>The addressed day-task is absent from the current user's day.</summary>
    public static readonly ErrorCode DayTaskNotFound = new("wellness.day_task.not_found");

    /// <summary>The days range query failed validation (missing or reversed bounds).</summary>
    public static readonly ErrorCode DayRangeValidation = new("wellness.day.range_validation");
}

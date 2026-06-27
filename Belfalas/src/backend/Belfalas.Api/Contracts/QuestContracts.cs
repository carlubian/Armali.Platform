namespace Belfalas.Api.Contracts;

public sealed record CompleteDailyQuestRequest(DateOnly CompletedOn);

public sealed record CompleteWeeklyQuestRequest(int WeekIndex);

public sealed record UpsertDailyHabitRequest(Guid AreaId, string Label, int Xp);

public sealed record UpsertWeeklyGoalRequest(Guid AreaId, string Label, int Xp);

public sealed record OverrideWeeklySetRequest(IReadOnlyList<Guid> WeeklyGoalIds);

public sealed record DailyHabitResponse(Guid Id, Guid EraId, Guid AreaId, string AreaName, string Label, int Xp);

public sealed record WeeklyGoalResponse(Guid Id, Guid EraId, Guid AreaId, string AreaName, string Label, int Xp);

/// <summary>A daily habit as the player sees it for the current day, with its completion state.</summary>
public sealed record DailyQuestResponse(
    Guid DailyHabitId,
    Guid AreaId,
    string AreaName,
    string Label,
    int Xp,
    bool Completed);

/// <summary>A weekly goal of the current set as the player sees it, with its completion state.</summary>
public sealed record WeeklyQuestResponse(
    Guid WeeklyGoalId,
    Guid AreaId,
    string AreaName,
    string Label,
    int Xp,
    bool Completed);

public sealed record WeeklySetResponse(
    Guid Id,
    Guid EraId,
    int WeekIndex,
    IReadOnlyList<WeeklyQuestResponse> Goals);

/// <summary>
/// The outcome of completing or un-completing an action: the resulting completion state
/// and the XP/level change applied to the action's area.
/// </summary>
public sealed record QuestCompletionResponse(
    Guid AreaId,
    string AreaName,
    bool Completed,
    int XpDelta,
    int AreaXp,
    int AreaLevel,
    int PreviousLevel,
    bool LevelChanged);

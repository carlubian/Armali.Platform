namespace Belfalas.Api.Contracts;

public sealed record CompleteDailyQuestRequest(DateOnly CompletedOn);

public sealed record CompleteWeeklyQuestRequest(int WeekIndex);

public sealed record UpsertDailyHabitRequest(Guid AreaId, string Label, int Xp);

public sealed record UpsertWeeklyGoalRequest(Guid AreaId, string Label, int Xp);

public sealed record OverrideWeeklySetRequest(IReadOnlyList<Guid> WeeklyGoalIds);

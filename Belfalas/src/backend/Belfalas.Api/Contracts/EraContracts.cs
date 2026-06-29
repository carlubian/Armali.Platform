namespace Belfalas.Api.Contracts;

public sealed record CreateEraRequest(
    string Name,
    DateOnly StartDate,
    int Weeks,
    string TemplateId,
    IReadOnlyList<CreateAreaRequest> Areas,
    IReadOnlyList<CreateDailyHabitDraftRequest>? DailyHabits = null,
    IReadOnlyList<CreateWeeklyGoalDraftRequest>? WeeklyGoals = null,
    int XpPerLevel = 100);

public sealed record CreateAreaRequest(string Name, int Order);

public sealed record CreateDailyHabitDraftRequest(int AreaOrder, string Label, int Xp);

public sealed record CreateWeeklyGoalDraftRequest(int AreaOrder, string Label, int Xp);

public sealed record EraSummaryResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    int Weeks,
    string Status,
    string TemplateId,
    int XpPerLevel);

public sealed record ArchivedEraSummaryResponse(
    Guid EraId,
    string Name,
    DateOnly StartDate,
    int Weeks,
    string TemplateId,
    int XpPerLevel,
    DateTimeOffset ArchivedAt);

public sealed record ArchivedEraResponse(
    Guid EraId,
    DateTimeOffset ArchivedAt,
    EraDetailResponse Era,
    ProgressionSummaryResponse Progression,
    WorldStateResponse World);

public sealed record EraDetailResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    int Weeks,
    string Status,
    string TemplateId,
    int XpPerLevel,
    IReadOnlyList<AreaResponse> Areas,
    IReadOnlyList<DailyHabitResponse> DailyHabits,
    IReadOnlyList<WeeklyGoalResponse> WeeklyGoals);

public sealed record AreaResponse(Guid Id, string Name, int Order, Guid? DistrictId);

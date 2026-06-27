using Belfalas.Api.Contracts;
using Belfalas.Domain;
using Belfalas.Persistence;
using Belfalas.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<BelfalasDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Belfalas")
        ?? "Data Source=/data/belfalas.db";
    options.UseSqlite(connectionString);
});

var app = builder.Build();

await app.Services.MigrateAndSeedBelfalasAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new HealthResponse("ok", "Belfalas.Api")))
    .WithName("GetHealth");

var api = app.MapGroup("/api");

MapEraRoutes(api.MapGroup("/eras"));
MapQuestRoutes(api.MapGroup("/quests"));
MapProgressionRoutes(api.MapGroup("/progression"));
MapWorldRoutes(api.MapGroup("/world"));
MapAdminRoutes(api.MapGroup("/admin"));

app.Run();

static void MapEraRoutes(RouteGroupBuilder eras)
{
    eras.MapGet("/", ListErasAsync).WithName("ListEras");
    eras.MapGet("/active", GetActiveEraAsync).WithName("GetActiveEra");
    eras.MapGet("/{eraId:guid}", GetEraAsync).WithName("GetEra");
    eras.MapPost("/", CreateEraAsync).WithName("CreateEra");
    eras.MapPost("/{eraId:guid}/archive", ArchiveEraAsync).WithName("ArchiveEra");
}

static void MapQuestRoutes(RouteGroupBuilder quests)
{
    quests.MapGet("/daily", GetTodaysDailyQuestsAsync).WithName("ListDailyQuests");
    quests.MapGet("/weekly", GetCurrentWeeklySetAsync).WithName("ListWeeklyQuests");
    quests.MapPost("/daily/{dailyHabitId:guid}/complete", CompleteDailyQuestAsync).WithName("CompleteDailyQuest");
    quests.MapDelete("/daily/{dailyHabitId:guid}/complete", UncompleteDailyQuestAsync).WithName("UncompleteDailyQuest");
    quests.MapPost("/weekly/{weeklyGoalId:guid}/complete", CompleteWeeklyQuestAsync).WithName("CompleteWeeklyQuest");
    quests.MapDelete("/weekly/{weeklyGoalId:guid}/complete", UncompleteWeeklyQuestAsync).WithName("UncompleteWeeklyQuest");
}

static void MapProgressionRoutes(RouteGroupBuilder progression)
{
    progression.MapGet("/summary", GetProgressionSummaryAsync).WithName("GetProgressionSummary");
    progression.MapGet("/areas/{areaId:guid}", GetAreaProgressionAsync).WithName("GetAreaProgression");
}

static void MapWorldRoutes(RouteGroupBuilder world)
{
    world.MapGet("/", GetActiveWorldStateAsync).WithName("GetWorldState");
    world.MapGet("/templates", ListWorldTemplatesAsync).WithName("ListWorldTemplates");
    world.MapGet("/eras/{eraId:guid}", GetEraWorldStateAsync).WithName("GetEraWorldState");
}

static void MapAdminRoutes(RouteGroupBuilder admin)
{
    admin.MapGet("/eras/{eraId:guid}/daily-habits", ListDailyHabitsAsync).WithName("AdminListDailyHabits");
    admin.MapPost("/eras/{eraId:guid}/daily-habits", CreateDailyHabitAsync).WithName("AdminCreateDailyHabit");
    admin.MapPut("/daily-habits/{dailyHabitId:guid}", UpdateDailyHabitAsync).WithName("AdminUpdateDailyHabit");
    admin.MapDelete("/daily-habits/{dailyHabitId:guid}", DeleteDailyHabitAsync).WithName("AdminDeleteDailyHabit");

    admin.MapGet("/eras/{eraId:guid}/weekly-goals", ListWeeklyGoalsAsync).WithName("AdminListWeeklyGoals");
    admin.MapPost("/eras/{eraId:guid}/weekly-goals", CreateWeeklyGoalAsync).WithName("AdminCreateWeeklyGoal");
    admin.MapPut("/weekly-goals/{weeklyGoalId:guid}", UpdateWeeklyGoalAsync).WithName("AdminUpdateWeeklyGoal");
    admin.MapDelete("/weekly-goals/{weeklyGoalId:guid}", DeleteWeeklyGoalAsync).WithName("AdminDeleteWeeklyGoal");

    admin.MapPut("/eras/{eraId:guid}/weekly-sets/{weekIndex:int}", OverrideWeeklySetAsync).WithName("AdminOverrideWeeklySet");
}

static async Task<IResult> ListErasAsync(BelfalasDbContext database, CancellationToken cancellationToken)
{
    var eras = await database.Eras
        .AsNoTracking()
        .OrderByDescending(era => era.StartDate)
        .ToListAsync(cancellationToken);

    return Results.Ok(eras.Select(ToEraSummary));
}

static async Task<IResult> GetActiveEraAsync(BelfalasDbContext database, CancellationToken cancellationToken)
{
    var era = await LoadEraDetailQuery(database)
        .FirstOrDefaultAsync(era => era.Status == EraStatus.Active, cancellationToken);

    return era is null ? Results.NotFound() : Results.Ok(ToEraDetail(era));
}

static async Task<IResult> GetEraAsync(Guid eraId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var era = await LoadEraDetailQuery(database)
        .FirstOrDefaultAsync(era => era.Id == eraId, cancellationToken);

    return era is null ? Results.NotFound() : Results.Ok(ToEraDetail(era));
}

static async Task<IResult> CreateEraAsync(CreateEraRequest request, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var validationProblem = ValidateCreateEraRequest(request);
    if (validationProblem is not null)
    {
        return validationProblem;
    }

    var hasActiveEra = await database.Eras.AnyAsync(era => era.Status == EraStatus.Active, cancellationToken);
    if (hasActiveEra)
    {
        return Conflict("Archive the current active era before creating a new one.");
    }

    var template = await database.WorldTemplates
        .Include(worldTemplate => worldTemplate.Districts)
        .FirstOrDefaultAsync(worldTemplate => worldTemplate.Id == request.TemplateId, cancellationToken);
    if (template is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["templateId"] = ["World template does not exist."],
        });
    }

    var areaDrafts = request.Areas.OrderBy(area => area.Order).ToList();
    if (template.Districts.Count < areaDrafts.Count)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["areas"] = ["World template does not have enough district slots for the requested areas."],
        });
    }

    var districtsBySlot = template.Districts.OrderBy(district => district.Slot).ToList();
    var areasByOrder = new Dictionary<int, Area>();
    var era = new Era
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        StartDate = request.StartDate,
        Weeks = request.Weeks,
        Status = EraStatus.Active,
        WorldTemplateId = request.TemplateId,
        XpPerLevel = request.XpPerLevel,
    };

    for (var index = 0; index < areaDrafts.Count; index++)
    {
        var draft = areaDrafts[index];
        var area = new Area
        {
            Id = Guid.NewGuid(),
            EraId = era.Id,
            Name = draft.Name.Trim(),
            Order = draft.Order,
            DistrictId = districtsBySlot[index].Id,
        };

        era.Areas.Add(area);
        areasByOrder.Add(area.Order, area);
    }

    foreach (var area in era.Areas)
    {
        database.AreaProgresses.Add(new AreaProgress
        {
            AreaId = area.Id,
            EraId = era.Id,
            Xp = 0,
            Level = 0,
        });
    }

    foreach (var draft in request.DailyHabits ?? [])
    {
        if (!areasByOrder.TryGetValue(draft.AreaOrder, out var area))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["dailyHabits"] = [$"Area order {draft.AreaOrder} does not exist."],
            });
        }

        era.DailyHabits.Add(new DailyHabit
        {
            Id = Guid.NewGuid(),
            EraId = era.Id,
            AreaId = area.Id,
            Area = area,
            Label = draft.Label.Trim(),
            Xp = draft.Xp,
        });
    }

    foreach (var draft in request.WeeklyGoals ?? [])
    {
        if (!areasByOrder.TryGetValue(draft.AreaOrder, out var area))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["weeklyGoals"] = [$"Area order {draft.AreaOrder} does not exist."],
            });
        }

        era.WeeklyGoals.Add(new WeeklyGoal
        {
            Id = Guid.NewGuid(),
            EraId = era.Id,
            AreaId = area.Id,
            Area = area,
            Label = draft.Label.Trim(),
            Xp = draft.Xp,
        });
    }

    database.Eras.Add(era);
    await database.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/eras/{era.Id}", ToEraDetail(era));
}

static async Task<IResult> ArchiveEraAsync(Guid eraId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var era = await LoadEraDetailQuery(database)
        .FirstOrDefaultAsync(era => era.Id == eraId, cancellationToken);
    if (era is null)
    {
        return Results.NotFound();
    }

    if (era.Status == EraStatus.Archived)
    {
        return Conflict("Era is already archived.");
    }

    var snapshot = await BuildArchiveSnapshotAsync(era, database, cancellationToken);
    era.Status = EraStatus.Archived;
    database.ArchivedEras.Add(new ArchivedEra
    {
        EraId = era.Id,
        ArchivedAt = DateTimeOffset.UtcNow,
        Snapshot = snapshot,
    });

    await database.SaveChangesAsync(cancellationToken);
    return Results.Ok(ToEraDetail(era));
}

static async Task<IResult> GetTodaysDailyQuestsAsync(BelfalasDbContext database, CancellationToken cancellationToken)
{
    var activeEra = await database.Eras
        .AsNoTracking()
        .FirstOrDefaultAsync(era => era.Status == EraStatus.Active, cancellationToken);
    if (activeEra is null)
    {
        return Results.NotFound();
    }

    var today = GetMadridToday();
    var habits = await database.DailyHabits
        .AsNoTracking()
        .Include(habit => habit.Area)
        .Where(habit => habit.EraId == activeEra.Id)
        .OrderBy(habit => habit.Area!.Order)
        .ThenBy(habit => habit.Label)
        .ToListAsync(cancellationToken);

    var completedToday = await database.DailyCompletions
        .Where(completion => completion.EraId == activeEra.Id && completion.Date == today)
        .Select(completion => completion.DailyHabitId)
        .ToHashSetAsync(cancellationToken);

    var quests = habits.Select(habit => new DailyQuestResponse(
        habit.Id,
        habit.AreaId,
        habit.Area?.Name ?? string.Empty,
        habit.Label,
        habit.Xp,
        completedToday.Contains(habit.Id)));

    return Results.Ok(quests);
}

static async Task<IResult> GetCurrentWeeklySetAsync(BelfalasDbContext database, CancellationToken cancellationToken)
{
    var activeEra = await database.Eras
        .AsNoTracking()
        .FirstOrDefaultAsync(era => era.Status == EraStatus.Active, cancellationToken);
    if (activeEra is null)
    {
        return Results.NotFound();
    }

    var weekIndex = GetCurrentWeekIndex(activeEra);
    if (weekIndex < 0 || weekIndex >= activeEra.Weeks)
    {
        return Results.NotFound();
    }

    return await GetOrCreateWeeklySetAsync(activeEra.Id, weekIndex, database, cancellationToken);
}

static async Task<IResult> ListDailyHabitsAsync(Guid eraId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    if (!await database.Eras.AnyAsync(era => era.Id == eraId, cancellationToken))
    {
        return Results.NotFound();
    }

    var habits = await database.DailyHabits
        .AsNoTracking()
        .Include(habit => habit.Area)
        .Where(habit => habit.EraId == eraId)
        .OrderBy(habit => habit.Area!.Order)
        .ThenBy(habit => habit.Label)
        .ToListAsync(cancellationToken);

    return Results.Ok(habits.Select(ToDailyHabitResponse));
}

static async Task<IResult> CreateDailyHabitAsync(
    Guid eraId,
    UpsertDailyHabitRequest request,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var validationProblem = ValidateQuestRequest(request.AreaId, request.Label, request.Xp);
    if (validationProblem is not null)
    {
        return validationProblem;
    }

    var areaResult = await ValidateEditableAreaAsync(eraId, request.AreaId, database, cancellationToken);
    if (areaResult is not null)
    {
        return areaResult;
    }

    var habit = new DailyHabit
    {
        Id = Guid.NewGuid(),
        EraId = eraId,
        AreaId = request.AreaId,
        Label = request.Label.Trim(),
        Xp = request.Xp,
    };

    database.DailyHabits.Add(habit);
    await database.SaveChangesAsync(cancellationToken);

    await database.Entry(habit).Reference(item => item.Area).LoadAsync(cancellationToken);
    return Results.Created($"/api/admin/daily-habits/{habit.Id}", ToDailyHabitResponse(habit));
}

static async Task<IResult> UpdateDailyHabitAsync(
    Guid dailyHabitId,
    UpsertDailyHabitRequest request,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var habit = await database.DailyHabits
        .Include(item => item.Era)
        .FirstOrDefaultAsync(item => item.Id == dailyHabitId, cancellationToken);
    if (habit is null)
    {
        return Results.NotFound();
    }

    var validationProblem = ValidateQuestRequest(request.AreaId, request.Label, request.Xp);
    if (validationProblem is not null)
    {
        return validationProblem;
    }

    if (habit.Era?.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    var areaResult = await ValidateEditableAreaAsync(habit.EraId, request.AreaId, database, cancellationToken);
    if (areaResult is not null)
    {
        return areaResult;
    }

    habit.AreaId = request.AreaId;
    habit.Label = request.Label.Trim();
    habit.Xp = request.Xp;

    await database.SaveChangesAsync(cancellationToken);
    await database.Entry(habit).Reference(item => item.Area).LoadAsync(cancellationToken);
    return Results.Ok(ToDailyHabitResponse(habit));
}

static async Task<IResult> DeleteDailyHabitAsync(Guid dailyHabitId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var habit = await database.DailyHabits
        .Include(item => item.Era)
        .FirstOrDefaultAsync(item => item.Id == dailyHabitId, cancellationToken);
    if (habit is null)
    {
        return Results.NotFound();
    }

    if (habit.Era?.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    database.DailyHabits.Remove(habit);
    await database.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
}

static async Task<IResult> ListWeeklyGoalsAsync(Guid eraId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    if (!await database.Eras.AnyAsync(era => era.Id == eraId, cancellationToken))
    {
        return Results.NotFound();
    }

    var goals = await database.WeeklyGoals
        .AsNoTracking()
        .Include(goal => goal.Area)
        .Where(goal => goal.EraId == eraId)
        .OrderBy(goal => goal.Area!.Order)
        .ThenBy(goal => goal.Label)
        .ToListAsync(cancellationToken);

    return Results.Ok(goals.Select(ToWeeklyGoalResponse));
}

static async Task<IResult> CreateWeeklyGoalAsync(
    Guid eraId,
    UpsertWeeklyGoalRequest request,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var validationProblem = ValidateQuestRequest(request.AreaId, request.Label, request.Xp);
    if (validationProblem is not null)
    {
        return validationProblem;
    }

    var areaResult = await ValidateEditableAreaAsync(eraId, request.AreaId, database, cancellationToken);
    if (areaResult is not null)
    {
        return areaResult;
    }

    var goal = new WeeklyGoal
    {
        Id = Guid.NewGuid(),
        EraId = eraId,
        AreaId = request.AreaId,
        Label = request.Label.Trim(),
        Xp = request.Xp,
    };

    database.WeeklyGoals.Add(goal);
    await database.SaveChangesAsync(cancellationToken);

    await database.Entry(goal).Reference(item => item.Area).LoadAsync(cancellationToken);
    return Results.Created($"/api/admin/weekly-goals/{goal.Id}", ToWeeklyGoalResponse(goal));
}

static async Task<IResult> UpdateWeeklyGoalAsync(
    Guid weeklyGoalId,
    UpsertWeeklyGoalRequest request,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var goal = await database.WeeklyGoals
        .Include(item => item.Era)
        .FirstOrDefaultAsync(item => item.Id == weeklyGoalId, cancellationToken);
    if (goal is null)
    {
        return Results.NotFound();
    }

    var validationProblem = ValidateQuestRequest(request.AreaId, request.Label, request.Xp);
    if (validationProblem is not null)
    {
        return validationProblem;
    }

    if (goal.Era?.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    var areaResult = await ValidateEditableAreaAsync(goal.EraId, request.AreaId, database, cancellationToken);
    if (areaResult is not null)
    {
        return areaResult;
    }

    goal.AreaId = request.AreaId;
    goal.Label = request.Label.Trim();
    goal.Xp = request.Xp;

    await database.SaveChangesAsync(cancellationToken);
    await database.Entry(goal).Reference(item => item.Area).LoadAsync(cancellationToken);
    return Results.Ok(ToWeeklyGoalResponse(goal));
}

static async Task<IResult> DeleteWeeklyGoalAsync(Guid weeklyGoalId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var goal = await database.WeeklyGoals
        .Include(item => item.Era)
        .FirstOrDefaultAsync(item => item.Id == weeklyGoalId, cancellationToken);
    if (goal is null)
    {
        return Results.NotFound();
    }

    if (goal.Era?.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    var setItems = database.WeeklySetItems.Where(item => item.WeeklyGoalId == weeklyGoalId);
    database.WeeklySetItems.RemoveRange(setItems);
    database.WeeklyGoals.Remove(goal);
    await database.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
}

static async Task<IResult> OverrideWeeklySetAsync(
    Guid eraId,
    int weekIndex,
    OverrideWeeklySetRequest request,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var era = await database.Eras.FirstOrDefaultAsync(item => item.Id == eraId, cancellationToken);
    if (era is null)
    {
        return Results.NotFound();
    }

    if (era.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    if (weekIndex < 0 || weekIndex >= era.Weeks)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["weekIndex"] = ["Week index is outside the era duration."],
        });
    }

    var goalIds = request.WeeklyGoalIds.Distinct().ToList();
    if (goalIds.Count != request.WeeklyGoalIds.Count)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["weeklyGoalIds"] = ["Duplicate weekly goals are not allowed."],
        });
    }

    var validGoalIds = await database.WeeklyGoals
        .Where(goal => goal.EraId == eraId && goalIds.Contains(goal.Id))
        .Select(goal => goal.Id)
        .ToListAsync(cancellationToken);
    if (validGoalIds.Count != goalIds.Count)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["weeklyGoalIds"] = ["Every weekly goal must belong to the target era."],
        });
    }

    var weeklySet = await database.WeeklySets
        .Include(set => set.Items)
        .FirstOrDefaultAsync(set => set.EraId == eraId && set.WeekIndex == weekIndex, cancellationToken);
    if (weeklySet is null)
    {
        weeklySet = new WeeklySet
        {
            Id = Guid.NewGuid(),
            EraId = eraId,
            WeekIndex = weekIndex,
        };
        database.WeeklySets.Add(weeklySet);
    }
    else
    {
        database.WeeklySetItems.RemoveRange(weeklySet.Items);
    }

    foreach (var goalId in goalIds)
    {
        weeklySet.Items.Add(new WeeklySetItem
        {
            WeeklySetId = weeklySet.Id,
            WeeklyGoalId = goalId,
        });
    }

    await database.SaveChangesAsync(cancellationToken);
    return await GetWeeklySetResponseAsync(weeklySet.Id, database, cancellationToken);
}

static async Task<IResult> GetOrCreateWeeklySetAsync(
    Guid eraId,
    int weekIndex,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var weeklySet = await EnsureWeeklySetAsync(eraId, weekIndex, database, cancellationToken);
    return await GetWeeklySetResponseAsync(weeklySet.Id, database, cancellationToken);
}

/// <summary>Returns the week's set, drawing it from the goal pool the first time it is requested.</summary>
static async Task<WeeklySet> EnsureWeeklySetAsync(
    Guid eraId,
    int weekIndex,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var weeklySet = await database.WeeklySets
        .FirstOrDefaultAsync(set => set.EraId == eraId && set.WeekIndex == weekIndex, cancellationToken);
    if (weeklySet is not null)
    {
        return weeklySet;
    }

    var selectedGoalIds = await DrawWeeklyGoalIdsAsync(eraId, weekIndex, database, cancellationToken);
    weeklySet = new WeeklySet
    {
        Id = Guid.NewGuid(),
        EraId = eraId,
        WeekIndex = weekIndex,
    };

    foreach (var goalId in selectedGoalIds)
    {
        weeklySet.Items.Add(new WeeklySetItem
        {
            WeeklySetId = weeklySet.Id,
            WeeklyGoalId = goalId,
        });
    }

    database.WeeklySets.Add(weeklySet);
    await database.SaveChangesAsync(cancellationToken);
    return weeklySet;
}

static async Task<IResult> GetWeeklySetResponseAsync(Guid weeklySetId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var weeklySet = await database.WeeklySets
        .AsNoTracking()
        .Include(set => set.Items)
        .ThenInclude(item => item.WeeklyGoal)
        .ThenInclude(goal => goal!.Area)
        .FirstAsync(set => set.Id == weeklySetId, cancellationToken);

    var completedThisWeek = await database.WeeklyCompletions
        .Where(completion => completion.EraId == weeklySet.EraId && completion.WeekIndex == weeklySet.WeekIndex)
        .Select(completion => completion.WeeklyGoalId)
        .ToHashSetAsync(cancellationToken);

    var goals = weeklySet.Items
        .Select(item => item.WeeklyGoal!)
        .OrderBy(goal => goal.Area!.Order)
        .ThenBy(goal => goal.Label)
        .Select(goal => new WeeklyQuestResponse(
            goal.Id,
            goal.AreaId,
            goal.Area?.Name ?? string.Empty,
            goal.Label,
            goal.Xp,
            completedThisWeek.Contains(goal.Id)))
        .ToList();

    return Results.Ok(new WeeklySetResponse(weeklySet.Id, weeklySet.EraId, weeklySet.WeekIndex, goals));
}

static async Task<IReadOnlyList<Guid>> DrawWeeklyGoalIdsAsync(
    Guid eraId,
    int weekIndex,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var goals = await database.WeeklyGoals
        .AsNoTracking()
        .Include(goal => goal.Area)
        .Where(goal => goal.EraId == eraId)
        .OrderBy(goal => goal.Area!.Order)
        .ThenBy(goal => goal.Label)
        .ToListAsync(cancellationToken);

    return goals
        .GroupBy(goal => goal.AreaId)
        .Select(group =>
        {
            var areaGoals = group.ToList();
            return areaGoals[weekIndex % areaGoals.Count].Id;
        })
        .ToList();
}

static async Task<IResult> CompleteDailyQuestAsync(
    Guid dailyHabitId,
    CompleteDailyQuestRequest request,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var habit = await database.DailyHabits
        .Include(item => item.Era)
        .Include(item => item.Area)
        .FirstOrDefaultAsync(item => item.Id == dailyHabitId, cancellationToken);
    if (habit is null)
    {
        return Results.NotFound();
    }

    if (habit.Era?.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    var today = GetMadridToday();
    if (request.CompletedOn != default && request.CompletedOn != today)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["completedOn"] = ["Daily quests can only be completed for the current day."],
        });
    }

    var alreadyDone = await database.DailyCompletions
        .AnyAsync(c => c.EraId == habit.EraId && c.Date == today && c.DailyHabitId == habit.Id, cancellationToken);
    if (alreadyDone)
    {
        return await ApplyCompletionAsync(habit.Area!, 0, completed: true, habit.Era!.XpPerLevel, database, cancellationToken);
    }

    database.DailyCompletions.Add(new DailyCompletion
    {
        Id = Guid.NewGuid(),
        EraId = habit.EraId,
        Date = today,
        DailyHabitId = habit.Id,
    });

    return await ApplyCompletionAsync(habit.Area!, habit.Xp, completed: true, habit.Era!.XpPerLevel, database, cancellationToken);
}

static async Task<IResult> UncompleteDailyQuestAsync(
    Guid dailyHabitId,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var habit = await database.DailyHabits
        .Include(item => item.Era)
        .Include(item => item.Area)
        .FirstOrDefaultAsync(item => item.Id == dailyHabitId, cancellationToken);
    if (habit is null)
    {
        return Results.NotFound();
    }

    if (habit.Era?.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    var today = GetMadridToday();
    var completion = await database.DailyCompletions
        .FirstOrDefaultAsync(c => c.EraId == habit.EraId && c.Date == today && c.DailyHabitId == habit.Id, cancellationToken);
    if (completion is null)
    {
        return await ApplyCompletionAsync(habit.Area!, 0, completed: false, habit.Era!.XpPerLevel, database, cancellationToken);
    }

    database.DailyCompletions.Remove(completion);
    return await ApplyCompletionAsync(habit.Area!, -habit.Xp, completed: false, habit.Era!.XpPerLevel, database, cancellationToken);
}

static async Task<IResult> CompleteWeeklyQuestAsync(
    Guid weeklyGoalId,
    CompleteWeeklyQuestRequest request,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var goal = await database.WeeklyGoals
        .Include(item => item.Era)
        .Include(item => item.Area)
        .FirstOrDefaultAsync(item => item.Id == weeklyGoalId, cancellationToken);
    if (goal is null)
    {
        return Results.NotFound();
    }

    if (goal.Era?.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    var era = goal.Era!;
    var weekIndex = GetCurrentWeekIndex(era);
    if (weekIndex < 0 || weekIndex >= era.Weeks)
    {
        return Conflict("The era has no active week right now.");
    }

    if (request.WeekIndex != weekIndex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["weekIndex"] = [$"Weekly quests can only be completed for the current week ({weekIndex})."],
        });
    }

    var weeklySet = await EnsureWeeklySetAsync(era.Id, weekIndex, database, cancellationToken);
    var inSet = await database.WeeklySetItems
        .AnyAsync(item => item.WeeklySetId == weeklySet.Id && item.WeeklyGoalId == goal.Id, cancellationToken);
    if (!inSet)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["weeklyGoalId"] = ["Goal is not part of the current weekly set."],
        });
    }

    var alreadyDone = await database.WeeklyCompletions
        .AnyAsync(c => c.EraId == era.Id && c.WeekIndex == weekIndex && c.WeeklyGoalId == goal.Id, cancellationToken);
    if (alreadyDone)
    {
        return await ApplyCompletionAsync(goal.Area!, 0, completed: true, era.XpPerLevel, database, cancellationToken);
    }

    database.WeeklyCompletions.Add(new WeeklyCompletion
    {
        Id = Guid.NewGuid(),
        EraId = era.Id,
        WeekIndex = weekIndex,
        WeeklyGoalId = goal.Id,
    });

    return await ApplyCompletionAsync(goal.Area!, goal.Xp, completed: true, era.XpPerLevel, database, cancellationToken);
}

static async Task<IResult> UncompleteWeeklyQuestAsync(
    Guid weeklyGoalId,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var goal = await database.WeeklyGoals
        .Include(item => item.Era)
        .Include(item => item.Area)
        .FirstOrDefaultAsync(item => item.Id == weeklyGoalId, cancellationToken);
    if (goal is null)
    {
        return Results.NotFound();
    }

    if (goal.Era?.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    var era = goal.Era!;
    var weekIndex = GetCurrentWeekIndex(era);
    if (weekIndex < 0 || weekIndex >= era.Weeks)
    {
        return Conflict("The era has no active week right now.");
    }

    var completion = await database.WeeklyCompletions
        .FirstOrDefaultAsync(c => c.EraId == era.Id && c.WeekIndex == weekIndex && c.WeeklyGoalId == goal.Id, cancellationToken);
    if (completion is null)
    {
        return await ApplyCompletionAsync(goal.Area!, 0, completed: false, era.XpPerLevel, database, cancellationToken);
    }

    database.WeeklyCompletions.Remove(completion);
    return await ApplyCompletionAsync(goal.Area!, -goal.Xp, completed: false, era.XpPerLevel, database, cancellationToken);
}

/// <summary>
/// Applies an XP delta to an area (clamped to [0, level-50 cap]), recomputes its level,
/// syncs the area's district evolution when the level changes, persists, and returns
/// the completion outcome.
/// </summary>
static async Task<IResult> ApplyCompletionAsync(
    Area area,
    int xpDelta,
    bool completed,
    int xpPerLevel,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var progress = await database.AreaProgresses
        .FirstOrDefaultAsync(item => item.AreaId == area.Id, cancellationToken);
    if (progress is null)
    {
        return Results.Problem(
            title: "Missing area progress",
            detail: "The area has no progression row; the era may be in an inconsistent state.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    var previousLevel = progress.Level;
    progress.Xp = Math.Clamp(progress.Xp + xpDelta, 0, Leveling.XpCap(xpPerLevel));
    progress.Level = Leveling.LevelForXp(progress.Xp, xpPerLevel);

    if (progress.Level != previousLevel)
    {
        var evolutionProblem = await SynchronizeDistrictEvolutionAsync(area, progress.Level, database, cancellationToken);
        if (evolutionProblem is not null)
        {
            return evolutionProblem;
        }
    }

    await database.SaveChangesAsync(cancellationToken);

    return Results.Ok(new QuestCompletionResponse(
        area.Id,
        area.Name,
        completed,
        xpDelta,
        progress.Xp,
        progress.Level,
        previousLevel,
        progress.Level != previousLevel));
}

static async Task<IResult?> SynchronizeDistrictEvolutionAsync(
    Area area,
    int targetLevel,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    if (area.DistrictId is null)
    {
        return Results.Problem(
            title: "Missing district",
            detail: "The area is not bound to a world district.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    var district = await database.Districts
        .Include(item => item.Plots)
        .Include(item => item.EvolutionStages)
        .FirstOrDefaultAsync(item => item.Id == area.DistrictId, cancellationToken);
    if (district is null)
    {
        return Results.Problem(
            title: "Missing district",
            detail: "The area's world district no longer exists.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    var targetStages = district.EvolutionStages
        .Where(stage => stage.Order <= targetLevel)
        .OrderBy(stage => stage.Order)
        .ToList();

    var targetBuildingCount = targetStages.Count(stage => stage.Kind == EvolutionStageKind.Building);
    var existingBuiltPlots = await database.BuiltPlots
        .Include(item => item.Plot)
        .Where(item => item.EraId == area.EraId && item.DistrictId == district.Id)
        .OrderBy(item => item.Plot!.PositionY)
        .ThenBy(item => item.Plot!.PositionX)
        .ToListAsync(cancellationToken);

    while (existingBuiltPlots.Count < targetBuildingCount)
    {
        var plot = PickNextOrganicPlot(district.Plots, existingBuiltPlots);
        if (plot is null)
        {
            return Results.Problem(
                title: "World template exhausted",
                detail: "The district has no free plot available for the requested evolution stage.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var variant = await PickRandomVariantAsync(district.WorldTemplateId, plot.Category, database, cancellationToken);
        if (variant is null)
        {
            return Results.Problem(
                title: "Missing variant",
                detail: $"World template has no variant for plot category '{plot.Category}'.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var builtPlot = new BuiltPlot
        {
            Id = Guid.NewGuid(),
            EraId = area.EraId,
            DistrictId = district.Id,
            PlotId = plot.Id,
            VariantId = variant.Id,
            Plot = plot,
            Variant = variant,
        };

        database.BuiltPlots.Add(builtPlot);
        existingBuiltPlots.Add(builtPlot);
    }

    if (existingBuiltPlots.Count > targetBuildingCount)
    {
        var surplusBuiltPlots = existingBuiltPlots
            .OrderByDescending(item => item.Plot!.PositionY)
            .ThenByDescending(item => item.Plot!.PositionX)
            .Take(existingBuiltPlots.Count - targetBuildingCount)
            .ToList();
        database.BuiltPlots.RemoveRange(surplusBuiltPlots);
    }

    var targetDenizenCounts = targetStages
        .Where(stage => stage.Kind == EvolutionStageKind.Denizen && !string.IsNullOrWhiteSpace(stage.DenizenType))
        .GroupBy(stage => stage.DenizenType!)
        .ToDictionary(group => group.Key, group => group.Count());

    var existingDenizens = await database.DenizenCounts
        .Where(item => item.EraId == area.EraId && item.DistrictId == district.Id)
        .ToListAsync(cancellationToken);

    foreach (var denizen in existingDenizens)
    {
        if (targetDenizenCounts.TryGetValue(denizen.DenizenType, out var count))
        {
            denizen.Count = count;
            targetDenizenCounts.Remove(denizen.DenizenType);
        }
        else
        {
            database.DenizenCounts.Remove(denizen);
        }
    }

    foreach (var (denizenType, count) in targetDenizenCounts)
    {
        database.DenizenCounts.Add(new DenizenCount
        {
            Id = Guid.NewGuid(),
            EraId = area.EraId,
            DistrictId = district.Id,
            DenizenType = denizenType,
            Count = count,
        });
    }

    return null;
}

static Plot? PickNextOrganicPlot(IEnumerable<Plot> plots, IReadOnlyCollection<BuiltPlot> builtPlots)
{
    var builtPlotIds = builtPlots.Select(item => item.PlotId).ToHashSet();
    var freePlots = plots
        .Where(plot => !builtPlotIds.Contains(plot.Id))
        .OrderBy(plot => plot.PositionY)
        .ThenBy(plot => plot.PositionX)
        .ToList();
    if (freePlots.Count == 0)
    {
        return null;
    }

    if (builtPlots.Count == 0)
    {
        var centerX = freePlots.Average(plot => plot.PositionX);
        var centerY = freePlots.Average(plot => plot.PositionY);
        var centeredPlots = freePlots
            .OrderBy(plot => Math.Abs(plot.PositionX - centerX) + Math.Abs(plot.PositionY - centerY))
            .ThenBy(plot => plot.PositionY)
            .ThenBy(plot => plot.PositionX)
            .ToList();

        return centeredPlots[Random.Shared.Next(centeredPlots.Count)];
    }

    var builtPositions = builtPlots
        .Where(item => item.Plot is not null)
        .Select(item => (item.Plot!.PositionX, item.Plot.PositionY))
        .ToHashSet();

    var adjacentPlots = freePlots
        .Where(plot => builtPositions.Any(position =>
            Math.Abs(position.PositionX - plot.PositionX) + Math.Abs(position.PositionY - plot.PositionY) == 1))
        .ToList();

    var candidates = adjacentPlots.Count == 0 ? freePlots : adjacentPlots;
    return candidates[Random.Shared.Next(candidates.Count)];
}

static async Task<Variant?> PickRandomVariantAsync(
    string worldTemplateId,
    string category,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var variants = await database.Variants
        .Where(item => item.WorldTemplateId == worldTemplateId && item.Category == category)
        .OrderBy(item => item.SpriteKey)
        .ToListAsync(cancellationToken);

    return variants.Count == 0 ? null : variants[Random.Shared.Next(variants.Count)];
}

static async Task<IResult> GetProgressionSummaryAsync(BelfalasDbContext database, CancellationToken cancellationToken)
{
    var era = await database.Eras
        .AsNoTracking()
        .Include(item => item.Areas)
        .FirstOrDefaultAsync(item => item.Status == EraStatus.Active, cancellationToken);
    if (era is null)
    {
        return Results.NotFound();
    }

    var progressByArea = await database.AreaProgresses
        .AsNoTracking()
        .Where(progress => progress.EraId == era.Id)
        .ToDictionaryAsync(progress => progress.AreaId, cancellationToken);

    var areas = era.Areas
        .OrderBy(area => area.Order)
        .Select(area => ToAreaProgressResponse(area, progressByArea.GetValueOrDefault(area.Id), era.XpPerLevel))
        .ToList();

    var globalLevel = areas.Count == 0 ? 0d : areas.Average(area => area.Level);

    return Results.Ok(new ProgressionSummaryResponse(era.Id, era.Name, globalLevel, Leveling.MaxLevel, areas));
}

static async Task<IResult> GetAreaProgressionAsync(Guid areaId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var area = await database.Areas
        .AsNoTracking()
        .Include(item => item.Era)
        .FirstOrDefaultAsync(item => item.Id == areaId, cancellationToken);
    if (area is null)
    {
        return Results.NotFound();
    }

    var progress = await database.AreaProgresses
        .AsNoTracking()
        .FirstOrDefaultAsync(item => item.AreaId == areaId, cancellationToken);

    return Results.Ok(ToAreaProgressResponse(area, progress, area.Era?.XpPerLevel ?? 100));
}

static async Task<IResult> ListWorldTemplatesAsync(BelfalasDbContext database, CancellationToken cancellationToken)
{
    var templates = await database.WorldTemplates
        .AsNoTracking()
        .Include(template => template.Districts)
        .ThenInclude(district => district.Plots)
        .Include(template => template.Districts)
        .ThenInclude(district => district.EvolutionStages)
        .Include(template => template.Variants)
        .AsSplitQuery()
        .OrderBy(template => template.Name)
        .ToListAsync(cancellationToken);

    return Results.Ok(templates.Select(ToWorldTemplateResponse));
}

static async Task<IResult> GetActiveWorldStateAsync(BelfalasDbContext database, CancellationToken cancellationToken)
{
    var activeEra = await database.Eras
        .AsNoTracking()
        .FirstOrDefaultAsync(era => era.Status == EraStatus.Active, cancellationToken);

    return activeEra is null
        ? Results.NotFound()
        : await GetEraWorldStateAsync(activeEra.Id, database, cancellationToken);
}

static async Task<IResult> GetEraWorldStateAsync(Guid eraId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var era = await database.Eras
        .AsNoTracking()
        .Include(item => item.Areas)
        .Include(item => item.WorldTemplate)
        .ThenInclude(template => template!.Districts)
        .ThenInclude(district => district.Plots)
        .Include(item => item.WorldTemplate)
        .ThenInclude(template => template!.Districts)
        .ThenInclude(district => district.EvolutionStages)
        .AsSplitQuery()
        .FirstOrDefaultAsync(item => item.Id == eraId, cancellationToken);
    if (era is null)
    {
        return Results.NotFound();
    }

    var progressByArea = await database.AreaProgresses
        .AsNoTracking()
        .Where(progress => progress.EraId == era.Id)
        .ToDictionaryAsync(progress => progress.AreaId, cancellationToken);

    var builtPlots = await database.BuiltPlots
        .AsNoTracking()
        .Include(plot => plot.Plot)
        .Include(plot => plot.Variant)
        .Where(plot => plot.EraId == era.Id)
        .OrderBy(plot => plot.DistrictId)
        .ThenBy(plot => plot.Plot!.PositionY)
        .ThenBy(plot => plot.Plot!.PositionX)
        .ToListAsync(cancellationToken);

    var denizens = await database.DenizenCounts
        .AsNoTracking()
        .Where(denizen => denizen.EraId == era.Id)
        .OrderBy(denizen => denizen.DistrictId)
        .ThenBy(denizen => denizen.DenizenType)
        .ToListAsync(cancellationToken);

    return Results.Ok(ToWorldStateResponse(era, progressByArea, builtPlots, denizens));
}

static AreaProgressResponse ToAreaProgressResponse(Area area, AreaProgress? progress, int xpPerLevel)
{
    var xp = progress?.Xp ?? 0;
    var level = progress?.Level ?? Leveling.LevelForXp(xp, xpPerLevel);

    return new AreaProgressResponse(
        area.Id,
        area.Name,
        area.Order,
        level,
        xp,
        xpPerLevel,
        Leveling.XpIntoLevel(xp, xpPerLevel),
        Leveling.XpForNextLevel(xp, xpPerLevel),
        Leveling.MaxLevel,
        level >= Leveling.MaxLevel);
}

static WorldTemplateResponse ToWorldTemplateResponse(WorldTemplate template) =>
    new(
        template.Id,
        template.Theme,
        template.Name,
        template.Districts
            .OrderBy(district => district.Slot)
            .Select(district => new WorldTemplateDistrictResponse(
                district.Id,
                district.Name,
                district.Slot,
                district.Plots
                    .OrderBy(plot => plot.PositionY)
                    .ThenBy(plot => plot.PositionX)
                    .Select(plot => new WorldTemplatePlotResponse(
                        plot.Id,
                        plot.Category,
                        plot.PositionX,
                        plot.PositionY))
                    .ToList(),
                district.EvolutionStages
                    .OrderBy(stage => stage.Order)
                    .Select(stage => new WorldTemplateEvolutionStageResponse(
                        stage.Id,
                        stage.Order,
                        stage.Kind.ToString(),
                        stage.DenizenType))
                    .ToList()))
            .ToList(),
        template.Variants
            .OrderBy(variant => variant.Category)
            .ThenBy(variant => variant.SpriteKey)
            .Select(variant => new WorldTemplateVariantResponse(
                variant.Id,
                variant.Category,
                variant.SpriteKey))
            .ToList());

static WorldStateResponse ToWorldStateResponse(
    Era era,
    IReadOnlyDictionary<Guid, AreaProgress> progressByArea,
    IReadOnlyList<BuiltPlot> builtPlots,
    IReadOnlyList<DenizenCount> denizens)
{
    var areasByDistrict = era.Areas
        .Where(area => area.DistrictId is not null)
        .ToDictionary(area => area.DistrictId!.Value);

    var builtPlotsByDistrict = builtPlots
        .GroupBy(plot => plot.DistrictId)
        .ToDictionary(group => group.Key, group => group.ToList());

    var denizensByDistrict = denizens
        .GroupBy(denizen => denizen.DistrictId)
        .ToDictionary(group => group.Key, group => group.ToList());

    var districts = era.WorldTemplate?.Districts
        .OrderBy(district => district.Slot)
        .Select(district =>
        {
            areasByDistrict.TryGetValue(district.Id, out var area);
            var areaLevel = area is not null && progressByArea.TryGetValue(area.Id, out var progress)
                ? progress.Level
                : 0;

            return new WorldDistrictStateResponse(
                district.Id,
                district.Name,
                district.Slot,
                area?.Id,
                area?.Name,
                areaLevel,
                builtPlotsByDistrict.GetValueOrDefault(district.Id, [])
                    .Where(plot => plot.Plot is not null && plot.Variant is not null)
                    .OrderBy(plot => plot.Plot!.PositionY)
                    .ThenBy(plot => plot.Plot!.PositionX)
                    .Select(plot => new BuiltPlotResponse(
                        plot.Id,
                        plot.PlotId,
                        plot.Plot!.Category,
                        plot.Plot.PositionX,
                        plot.Plot.PositionY,
                        plot.VariantId,
                        plot.Variant!.SpriteKey))
                    .ToList(),
                denizensByDistrict.GetValueOrDefault(district.Id, [])
                    .OrderBy(denizen => denizen.DenizenType)
                    .Select(denizen => new DenizenCountResponse(
                        denizen.DenizenType,
                        denizen.Count))
                    .ToList());
        })
        .ToList() ?? [];

    return new WorldStateResponse(era.Id, era.Name, era.WorldTemplateId, districts);
}

static IQueryable<Era> LoadEraDetailQuery(BelfalasDbContext database) =>
    database.Eras
        .Include(era => era.Areas)
        .Include(era => era.DailyHabits)
        .ThenInclude(habit => habit.Area)
        .Include(era => era.WeeklyGoals)
        .ThenInclude(goal => goal.Area)
        .AsSplitQuery();

static IResult? ValidateCreateEraRequest(CreateEraRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        errors["name"] = ["Name is required."];
    }

    if (request.Weeks <= 0)
    {
        errors["weeks"] = ["Weeks must be positive."];
    }

    if (request.XpPerLevel <= 0)
    {
        errors["xpPerLevel"] = ["XP per level must be positive."];
    }

    if (string.IsNullOrWhiteSpace(request.TemplateId))
    {
        errors["templateId"] = ["Template id is required."];
    }

    if (request.Areas.Count == 0)
    {
        errors["areas"] = ["At least one area is required."];
    }
    else if (request.Areas.Select(area => area.Order).Distinct().Count() != request.Areas.Count)
    {
        errors["areas"] = ["Area orders must be unique."];
    }
    else if (request.Areas.Any(area => string.IsNullOrWhiteSpace(area.Name)))
    {
        errors["areas"] = ["Every area needs a name."];
    }

    AddQuestDraftErrors(errors, "dailyHabits", request.DailyHabits, draft => draft.Label, draft => draft.Xp);
    AddQuestDraftErrors(errors, "weeklyGoals", request.WeeklyGoals, draft => draft.Label, draft => draft.Xp);

    return errors.Count == 0 ? null : Results.ValidationProblem(errors);
}

static void AddQuestDraftErrors<TDraft>(
    Dictionary<string, string[]> errors,
    string key,
    IReadOnlyList<TDraft>? drafts,
    Func<TDraft, string> labelSelector,
    Func<TDraft, int> xpSelector)
{
    if (drafts is null)
    {
        return;
    }

    foreach (var draft in drafts)
    {
        if (string.IsNullOrWhiteSpace(labelSelector(draft)) || xpSelector(draft) <= 0)
        {
            errors[key] = ["Quest drafts need a label and positive XP."];
            return;
        }
    }
}

static IResult? ValidateQuestRequest(Guid areaId, string label, int xp)
{
    var errors = new Dictionary<string, string[]>();
    if (areaId == Guid.Empty)
    {
        errors["areaId"] = ["Area id is required."];
    }

    if (string.IsNullOrWhiteSpace(label))
    {
        errors["label"] = ["Label is required."];
    }

    if (xp <= 0)
    {
        errors["xp"] = ["XP must be positive."];
    }

    return errors.Count == 0 ? null : Results.ValidationProblem(errors);
}

static async Task<IResult?> ValidateEditableAreaAsync(
    Guid eraId,
    Guid areaId,
    BelfalasDbContext database,
    CancellationToken cancellationToken)
{
    var era = await database.Eras.AsNoTracking().FirstOrDefaultAsync(item => item.Id == eraId, cancellationToken);
    if (era is null)
    {
        return Results.NotFound();
    }

    if (era.Status == EraStatus.Archived)
    {
        return Conflict("Archived eras are read-only.");
    }

    var areaExists = await database.Areas.AnyAsync(area => area.EraId == eraId && area.Id == areaId, cancellationToken);
    if (!areaExists)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["areaId"] = ["Area must belong to the target era."],
        });
    }

    return null;
}

static int GetCurrentWeekIndex(Era era)
{
    var elapsedDays = GetMadridToday().DayNumber - era.StartDate.DayNumber;
    return elapsedDays < 0 ? -1 : elapsedDays / 7;
}

static DateOnly GetMadridToday() =>
    DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, GetMadridTimeZone()).Date);

static TimeZoneInfo GetMadridTimeZone()
{
    try
    {
        return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    }
    catch (TimeZoneNotFoundException)
    {
        return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
    }
}

static async Task<string> BuildArchiveSnapshotAsync(Era era, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var areaProgress = await database.AreaProgresses
        .AsNoTracking()
        .Where(progress => progress.EraId == era.Id)
        .OrderBy(progress => progress.AreaId)
        .ToListAsync(cancellationToken);
    var weeklySets = await database.WeeklySets
        .AsNoTracking()
        .Include(set => set.Items)
        .Where(set => set.EraId == era.Id)
        .OrderBy(set => set.WeekIndex)
        .ToListAsync(cancellationToken);
    var builtPlots = await database.BuiltPlots
        .AsNoTracking()
        .Where(plot => plot.EraId == era.Id)
        .OrderBy(plot => plot.DistrictId)
        .ThenBy(plot => plot.PlotId)
        .ToListAsync(cancellationToken);
    var denizens = await database.DenizenCounts
        .AsNoTracking()
        .Where(denizen => denizen.EraId == era.Id)
        .OrderBy(denizen => denizen.DistrictId)
        .ThenBy(denizen => denizen.DenizenType)
        .ToListAsync(cancellationToken);

    var snapshot = new
    {
        Era = ToEraDetail(era),
        AreaProgress = areaProgress.Select(progress => new { progress.AreaId, progress.Xp, progress.Level }),
        WeeklySets = weeklySets.Select(set => new
        {
            set.WeekIndex,
            WeeklyGoalIds = set.Items.Select(item => item.WeeklyGoalId).OrderBy(id => id),
        }),
        BuiltPlots = builtPlots.Select(plot => new { plot.DistrictId, plot.PlotId, plot.VariantId }),
        Denizens = denizens.Select(denizen => new { denizen.DistrictId, denizen.DenizenType, denizen.Count }),
    };

    return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false });
}

static EraSummaryResponse ToEraSummary(Era era) =>
    new(era.Id, era.Name, era.StartDate, era.Weeks, era.Status.ToString(), era.WorldTemplateId, era.XpPerLevel);

static EraDetailResponse ToEraDetail(Era era) =>
    new(
        era.Id,
        era.Name,
        era.StartDate,
        era.Weeks,
        era.Status.ToString(),
        era.WorldTemplateId,
        era.XpPerLevel,
        era.Areas.OrderBy(area => area.Order).Select(ToAreaResponse).ToList(),
        era.DailyHabits.OrderBy(habit => habit.Area?.Order).ThenBy(habit => habit.Label).Select(ToDailyHabitResponse).ToList(),
        era.WeeklyGoals.OrderBy(goal => goal.Area?.Order).ThenBy(goal => goal.Label).Select(ToWeeklyGoalResponse).ToList());

static AreaResponse ToAreaResponse(Area area) =>
    new(area.Id, area.Name, area.Order, area.DistrictId);

static DailyHabitResponse ToDailyHabitResponse(DailyHabit habit) =>
    new(habit.Id, habit.EraId, habit.AreaId, habit.Area?.Name ?? string.Empty, habit.Label, habit.Xp);

static WeeklyGoalResponse ToWeeklyGoalResponse(WeeklyGoal goal) =>
    new(goal.Id, goal.EraId, goal.AreaId, goal.Area?.Name ?? string.Empty, goal.Label, goal.Xp);

static IResult Conflict(string detail) =>
    Results.Problem(title: "Conflict", detail: detail, statusCode: StatusCodes.Status409Conflict);

/// <summary>Exposed so integration tests can host the API via <c>WebApplicationFactory</c>.</summary>
public partial class Program;

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

static IResult NotImplemented(string routeName) =>
    Results.Problem(
        title: "Not implemented",
        detail: $"{routeName} is part of the Wave 0 frozen API surface and will be implemented in a later wave.",
        statusCode: StatusCodes.Status501NotImplemented);

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
    quests.MapGet("/daily", ListActiveDailyHabitsAsync).WithName("ListDailyQuests");
    quests.MapGet("/weekly", GetCurrentWeeklySetAsync).WithName("ListWeeklyQuests");
    quests.MapPost("/daily/{dailyHabitId:guid}/complete", (Guid dailyHabitId, CompleteDailyQuestRequest request) => NotImplemented("Complete daily quest")).WithName("CompleteDailyQuest");
    quests.MapPost("/weekly/{weeklyGoalId:guid}/complete", (Guid weeklyGoalId, CompleteWeeklyQuestRequest request) => NotImplemented("Complete weekly quest")).WithName("CompleteWeeklyQuest");
}

static void MapProgressionRoutes(RouteGroupBuilder progression)
{
    progression.MapGet("/summary", () => NotImplemented("Get progression summary")).WithName("GetProgressionSummary");
    progression.MapGet("/areas/{areaId:guid}", (Guid areaId) => NotImplemented("Get area progression")).WithName("GetAreaProgression");
}

static void MapWorldRoutes(RouteGroupBuilder world)
{
    world.MapGet("/", () => NotImplemented("Get active world state")).WithName("GetWorldState");
    world.MapGet("/templates", () => NotImplemented("List world templates")).WithName("ListWorldTemplates");
    world.MapGet("/eras/{eraId:guid}", (Guid eraId) => NotImplemented("Get era world state")).WithName("GetEraWorldState");
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

static async Task<IResult> ListActiveDailyHabitsAsync(BelfalasDbContext database, CancellationToken cancellationToken)
{
    var activeEra = await database.Eras
        .AsNoTracking()
        .FirstOrDefaultAsync(era => era.Status == EraStatus.Active, cancellationToken);
    if (activeEra is null)
    {
        return Results.NotFound();
    }

    return await ListDailyHabitsAsync(activeEra.Id, database, cancellationToken);
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
    var weeklySet = await database.WeeklySets
        .AsNoTracking()
        .FirstOrDefaultAsync(set => set.EraId == eraId && set.WeekIndex == weekIndex, cancellationToken);
    if (weeklySet is not null)
    {
        return await GetWeeklySetResponseAsync(weeklySet.Id, database, cancellationToken);
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
    return await GetWeeklySetResponseAsync(weeklySet.Id, database, cancellationToken);
}

static async Task<IResult> GetWeeklySetResponseAsync(Guid weeklySetId, BelfalasDbContext database, CancellationToken cancellationToken)
{
    var weeklySet = await database.WeeklySets
        .AsNoTracking()
        .Include(set => set.Items)
        .ThenInclude(item => item.WeeklyGoal)
        .ThenInclude(goal => goal!.Area)
        .FirstAsync(set => set.Id == weeklySetId, cancellationToken);

    var goals = weeklySet.Items
        .Select(item => item.WeeklyGoal!)
        .OrderBy(goal => goal.Area!.Order)
        .ThenBy(goal => goal.Label)
        .Select(ToWeeklyGoalResponse)
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
    var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, GetMadridTimeZone()).Date);
    var elapsedDays = today.DayNumber - era.StartDate.DayNumber;
    return elapsedDays < 0 ? -1 : elapsedDays / 7;
}

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
    new(era.Id, era.Name, era.StartDate, era.Weeks, era.Status.ToString(), era.WorldTemplateId);

static EraDetailResponse ToEraDetail(Era era) =>
    new(
        era.Id,
        era.Name,
        era.StartDate,
        era.Weeks,
        era.Status.ToString(),
        era.WorldTemplateId,
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

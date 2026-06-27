using Belfalas.Api.Contracts;
using Belfalas.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<BelfalasDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Belfalas")
        ?? "Data Source=/data/belfalas.db";
    options.UseSqlite(connectionString);
});

var app = builder.Build();

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
    eras.MapGet("/", () => NotImplemented("List eras")).WithName("ListEras");
    eras.MapGet("/active", () => NotImplemented("Get active era")).WithName("GetActiveEra");
    eras.MapGet("/{eraId:guid}", (Guid eraId) => NotImplemented("Get era")).WithName("GetEra");
    eras.MapPost("/", (CreateEraRequest request) => NotImplemented("Create era")).WithName("CreateEra");
    eras.MapPost("/{eraId:guid}/archive", (Guid eraId) => NotImplemented("Archive era")).WithName("ArchiveEra");
}

static void MapQuestRoutes(RouteGroupBuilder quests)
{
    quests.MapGet("/daily", () => NotImplemented("List today's daily habits")).WithName("ListDailyQuests");
    quests.MapGet("/weekly", () => NotImplemented("List current weekly goals")).WithName("ListWeeklyQuests");
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
    admin.MapGet("/eras/{eraId:guid}/daily-habits", (Guid eraId) => NotImplemented("List daily habits")).WithName("AdminListDailyHabits");
    admin.MapPost("/eras/{eraId:guid}/daily-habits", (Guid eraId, UpsertDailyHabitRequest request) => NotImplemented("Create daily habit")).WithName("AdminCreateDailyHabit");
    admin.MapPut("/daily-habits/{dailyHabitId:guid}", (Guid dailyHabitId, UpsertDailyHabitRequest request) => NotImplemented("Update daily habit")).WithName("AdminUpdateDailyHabit");
    admin.MapDelete("/daily-habits/{dailyHabitId:guid}", (Guid dailyHabitId) => NotImplemented("Delete daily habit")).WithName("AdminDeleteDailyHabit");

    admin.MapGet("/eras/{eraId:guid}/weekly-goals", (Guid eraId) => NotImplemented("List weekly goal pool")).WithName("AdminListWeeklyGoals");
    admin.MapPost("/eras/{eraId:guid}/weekly-goals", (Guid eraId, UpsertWeeklyGoalRequest request) => NotImplemented("Create weekly goal")).WithName("AdminCreateWeeklyGoal");
    admin.MapPut("/weekly-goals/{weeklyGoalId:guid}", (Guid weeklyGoalId, UpsertWeeklyGoalRequest request) => NotImplemented("Update weekly goal")).WithName("AdminUpdateWeeklyGoal");
    admin.MapDelete("/weekly-goals/{weeklyGoalId:guid}", (Guid weeklyGoalId) => NotImplemented("Delete weekly goal")).WithName("AdminDeleteWeeklyGoal");

    admin.MapPut("/eras/{eraId:guid}/weekly-sets/{weekIndex:int}", (Guid eraId, int weekIndex, OverrideWeeklySetRequest request) => NotImplemented("Override weekly set")).WithName("AdminOverrideWeeklySet");
}

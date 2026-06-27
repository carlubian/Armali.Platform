using System.Net;
using System.Net.Http.Json;
using Belfalas.Api.Contracts;

namespace Belfalas.Tests;

public sealed class ProgressionApiTests
{
    private const string TemplateId = "tropical-v1";

    [Fact]
    public async Task Completing_a_daily_habit_credits_xp_and_levels_up_immediately()
    {
        using var factory = new BelfalasApiFactory();
        var client = factory.CreateClient();

        var era = await CreateEraAsync(
            client,
            xpPerLevel: 15,
            areas: [new CreateAreaRequest("Work", 1)],
            dailyHabits: [new CreateDailyHabitDraftRequest(1, "Email", 20)]);
        var habit = era.DailyHabits.Single();

        var daily = await client.GetFromJsonAsync<List<DailyQuestResponse>>("/api/quests/daily");
        Assert.NotNull(daily);
        Assert.False(Assert.Single(daily).Completed);

        var first = await CompleteDailyAsync(client, habit.Id);
        Assert.True(first.Completed);
        Assert.Equal(20, first.XpDelta);
        Assert.Equal(20, first.AreaXp);
        Assert.Equal(1, first.AreaLevel);
        Assert.Equal(0, first.PreviousLevel);
        Assert.True(first.LevelChanged);

        // Re-completing the same day is idempotent: no extra XP.
        var second = await CompleteDailyAsync(client, habit.Id);
        Assert.Equal(0, second.XpDelta);
        Assert.Equal(20, second.AreaXp);
        Assert.Equal(1, second.AreaLevel);
    }

    [Fact]
    public async Task Uncompleting_a_daily_habit_reverts_xp_and_level()
    {
        using var factory = new BelfalasApiFactory();
        var client = factory.CreateClient();

        var era = await CreateEraAsync(
            client,
            xpPerLevel: 15,
            areas: [new CreateAreaRequest("Work", 1)],
            dailyHabits: [new CreateDailyHabitDraftRequest(1, "Email", 20)]);
        var habit = era.DailyHabits.Single();

        await CompleteDailyAsync(client, habit.Id);

        var response = await client.DeleteAsync($"/api/quests/daily/{habit.Id}/complete");
        var reverted = await ReadCompletionAsync(response);
        Assert.False(reverted.Completed);
        Assert.Equal(-20, reverted.XpDelta);
        Assert.Equal(0, reverted.AreaXp);
        Assert.Equal(0, reverted.AreaLevel);
        Assert.Equal(1, reverted.PreviousLevel);

        // Un-completing again is idempotent.
        var again = await ReadCompletionAsync(await client.DeleteAsync($"/api/quests/daily/{habit.Id}/complete"));
        Assert.Equal(0, again.XpDelta);
        Assert.Equal(0, again.AreaXp);
    }

    [Fact]
    public async Task Weekly_completion_is_rejected_for_a_goal_outside_the_current_set()
    {
        using var factory = new BelfalasApiFactory();
        var client = factory.CreateClient();

        // Two goals in the same area: only one is drawn into the week's set.
        var era = await CreateEraAsync(
            client,
            xpPerLevel: 100,
            areas: [new CreateAreaRequest("Work", 1)],
            weeklyGoals:
            [
                new CreateWeeklyGoalDraftRequest(1, "Goal A", 40),
                new CreateWeeklyGoalDraftRequest(1, "Goal B", 40),
            ]);

        var set = await client.GetFromJsonAsync<WeeklySetResponse>("/api/quests/weekly");
        Assert.NotNull(set);
        var inSet = Assert.Single(set.Goals);
        var outOfSet = era.WeeklyGoals.Single(goal => goal.Id != inSet.WeeklyGoalId);

        var rejected = await client.PostAsJsonAsync(
            $"/api/quests/weekly/{outOfSet.Id}/complete",
            new CompleteWeeklyQuestRequest(set.WeekIndex));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var accepted = await client.PostAsJsonAsync(
            $"/api/quests/weekly/{inSet.WeeklyGoalId}/complete",
            new CompleteWeeklyQuestRequest(set.WeekIndex));
        var result = await ReadCompletionAsync(accepted);
        Assert.True(result.Completed);
        Assert.Equal(40, result.AreaXp);
    }

    [Fact]
    public async Task Progression_summary_reports_the_average_global_level()
    {
        using var factory = new BelfalasApiFactory();
        var client = factory.CreateClient();

        var era = await CreateEraAsync(
            client,
            xpPerLevel: 15,
            areas: [new CreateAreaRequest("Work", 1), new CreateAreaRequest("Social", 2)],
            dailyHabits:
            [
                new CreateDailyHabitDraftRequest(1, "Email", 20),
                new CreateDailyHabitDraftRequest(2, "Call a friend", 5),
            ]);

        foreach (var habit in era.DailyHabits)
        {
            await CompleteDailyAsync(client, habit.Id);
        }

        var summary = await client.GetFromJsonAsync<ProgressionSummaryResponse>("/api/progression/summary");
        Assert.NotNull(summary);
        Assert.Equal(2, summary.Areas.Count);
        Assert.Equal(Leveling_MaxLevel, summary.MaxLevel);

        // Work: 20 XP -> level 1; Social: 5 XP -> level 0; average = 0.5.
        Assert.Equal(0.5, summary.GlobalLevel, precision: 3);

        var work = summary.Areas.Single(area => area.AreaName == "Work");
        Assert.Equal(1, work.Level);
        Assert.Equal(20, work.Xp);
        Assert.Equal(5, work.XpIntoLevel);
        Assert.Equal(10, work.XpForNextLevel);
    }

    private const int Leveling_MaxLevel = 50;

    private static async Task<EraDetailResponse> CreateEraAsync(
        HttpClient client,
        int xpPerLevel,
        IReadOnlyList<CreateAreaRequest> areas,
        IReadOnlyList<CreateDailyHabitDraftRequest>? dailyHabits = null,
        IReadOnlyList<CreateWeeklyGoalDraftRequest>? weeklyGoals = null)
    {
        // Start two days ago so the current week is deterministically week 0.
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        var request = new CreateEraRequest(
            "Test Era",
            startDate,
            Weeks: 50,
            TemplateId,
            areas,
            dailyHabits,
            weeklyGoals,
            xpPerLevel);

        var response = await client.PostAsJsonAsync("/api/eras", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var era = await response.Content.ReadFromJsonAsync<EraDetailResponse>();
        Assert.NotNull(era);
        return era;
    }

    private static async Task<QuestCompletionResponse> CompleteDailyAsync(HttpClient client, Guid habitId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/quests/daily/{habitId}/complete",
            new CompleteDailyQuestRequest(default));
        return await ReadCompletionAsync(response);
    }

    private static async Task<QuestCompletionResponse> ReadCompletionAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QuestCompletionResponse>();
        Assert.NotNull(result);
        return result;
    }
}

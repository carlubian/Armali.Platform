using System.Text.Json;
using Segaris.Api.Modules.Wellness;
using Segaris.Api.Modules.Wellness.Contracts;
using Segaris.Api.Modules.Wellness.Domain;

namespace Segaris.UnitTests;

public sealed class WellnessContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Category_enum_values_are_frozen()
    {
        Assert.Equal(
            ["HealthAndBody", "MindAndSleep", "PeopleAndWork"],
            Enum.GetNames<WellnessCategory>());
        Assert.Equal(
            [0, 1, 2],
            Enum.GetValues<WellnessCategory>().Select(value => (int)value).ToArray());
    }

    [Fact]
    public void Defaults_and_validation_bounds_are_frozen()
    {
        Assert.Equal(200, WellnessDefaults.TaskNameMaximumLength);
        Assert.Equal(6, WellnessDefaults.DailyTaskCount);
        Assert.Equal((0, 100), (WellnessDefaults.MinimumScore, WellnessDefaults.MaximumScore));
        Assert.False(WellnessDefaults.TaskCompleted);
    }

    [Fact]
    public void Routes_freeze_today_toggle_days_and_task_catalogue()
    {
        Assert.Equal("wellness", WellnessApiRoutes.Wellness);
        Assert.Equal("wellness/today", WellnessApiRoutes.Today);
        Assert.Equal("wellness/today/tasks/{dayTaskId:int}/toggle", WellnessApiRoutes.TodayTaskToggle);
        Assert.Equal("wellness/days", WellnessApiRoutes.Days);
        Assert.Equal("from", WellnessApiRoutes.FromQuery);
        Assert.Equal("to", WellnessApiRoutes.ToQuery);

        // The task catalogue follows the module-owned {owner}/{catalog} convention.
        Assert.Equal("wellness/tasks", WellnessApiRoutes.TaskCatalogue);
        Assert.Equal("/{taskId:int}", WellnessApiRoutes.TaskById);
    }

    [Fact]
    public void Configuration_facing_catalog_contracts_are_explicit()
    {
        Assert.Empty(WellnessConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [WellnessCatalogKind.Tasks],
            WellnessConfigurationContracts.OwnedCatalogs.Select(descriptor => descriptor.Kind).ToArray());

        // The task catalogue is optional (may be empty); deletion is impact-free because
        // days hold task snapshots, so it is never referenced and supports no clearing.
        Assert.Equal(("tasks", false, false), (
            WellnessConfigurationContracts.OwnedCatalogs[0].RouteSegment,
            WellnessConfigurationContracts.OwnedCatalogs[0].IsRequired,
            WellnessConfigurationContracts.OwnedCatalogs[0].SupportsClearing));
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("wellness.task.not_found", WellnessErrorCodes.TaskNotFound.Value);
        Assert.Equal("wellness.task.validation", WellnessErrorCodes.TaskValidation.Value);
        Assert.Equal("wellness.day_task.not_found", WellnessErrorCodes.DayTaskNotFound.Value);
        Assert.Equal("wellness.day.range_validation", WellnessErrorCodes.DayRangeValidation.Value);
    }

    [Fact]
    public void Today_response_serializes_to_the_frozen_wire_shape()
    {
        var today = new WellnessTodayResponse(
            new DateOnly(2026, 7, 13),
            67,
            [new WellnessDayTaskResponse(5, "Drink water", "HealthAndBody", true, 0)]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(today, Web));
        var root = document.RootElement;

        Assert.Equal("2026-07-13", root.GetProperty("date").GetString());
        Assert.Equal(67, root.GetProperty("score").GetInt32());
        var task = root.GetProperty("tasks")[0];
        Assert.Equal(5, task.GetProperty("id").GetInt32());
        Assert.Equal("HealthAndBody", task.GetProperty("category").GetString());
        Assert.True(task.GetProperty("completed").GetBoolean());
        Assert.Equal(0, task.GetProperty("position").GetInt32());
    }

    [Fact]
    public void Empty_day_reports_a_null_score()
    {
        // A day with no tasks (an empty catalogue) carries no score; a visited day with
        // zero completed tasks reports 0 rather than null.
        var empty = new WellnessTodayResponse(new DateOnly(2026, 7, 13), null, []);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(empty, Web));
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("score").ValueKind);
        Assert.Empty(document.RootElement.GetProperty("tasks").EnumerateArray());
    }

    [Fact]
    public void Day_range_response_carries_per_day_scores()
    {
        var list = new WellnessDayListResponse(
            new DateOnly(2026, 7, 6),
            new DateOnly(2026, 7, 12),
            [new WellnessDayScoreResponse(new DateOnly(2026, 7, 6), 50)]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(list, Web));
        var root = document.RootElement;

        Assert.Equal("2026-07-06", root.GetProperty("from").GetString());
        Assert.Equal("2026-07-12", root.GetProperty("to").GetString());
        var day = root.GetProperty("days")[0];
        Assert.Equal("2026-07-06", day.GetProperty("date").GetString());
        Assert.Equal(50, day.GetProperty("score").GetInt32());
    }
}

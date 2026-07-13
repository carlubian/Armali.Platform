using Segaris.Api.Modules.Wellness.Domain;

namespace Segaris.UnitTests;

public sealed class WellnessDaySelectionTests
{
    [Fact]
    public void Selector_returns_empty_selection_for_empty_catalogue()
    {
        var selector = new WellnessDaySelector(new Random(1));

        Assert.Empty(selector.Select([]));
    }

    [Fact]
    public void Selector_includes_all_tasks_when_catalogue_has_six_or_fewer()
    {
        var catalogue = Tasks(
            WellnessCategory.HealthAndBody,
            WellnessCategory.HealthAndBody,
            WellnessCategory.MindAndSleep,
            WellnessCategory.PeopleAndWork);
        var selector = new WellnessDaySelector(new Random(1));

        var selected = selector.Select(catalogue);

        Assert.Equal(catalogue.Select(task => task.Name).Order(), selected.Select(task => task.Name).Order());
    }

    [Fact]
    public void Selector_picks_six_tasks_with_at_least_one_from_each_available_category()
    {
        var catalogue = Tasks(
            WellnessCategory.HealthAndBody,
            WellnessCategory.HealthAndBody,
            WellnessCategory.HealthAndBody,
            WellnessCategory.MindAndSleep,
            WellnessCategory.MindAndSleep,
            WellnessCategory.MindAndSleep,
            WellnessCategory.PeopleAndWork,
            WellnessCategory.PeopleAndWork,
            WellnessCategory.PeopleAndWork);
        var selector = new WellnessDaySelector(new Random(2));

        var selected = selector.Select(catalogue);

        Assert.Equal(WellnessDefaults.DailyTaskCount, selected.Count);
        Assert.Contains(selected, task => task.Category == WellnessCategory.HealthAndBody);
        Assert.Contains(selected, task => task.Category == WellnessCategory.MindAndSleep);
        Assert.Contains(selected, task => task.Category == WellnessCategory.PeopleAndWork);
        Assert.Equal(selected.Count, selected.Select(task => task.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Selector_handles_more_categories_than_slots()
    {
        var catalogue = Enumerable.Range(0, 8)
            .Select(index => new WellnessSelectableTask(index + 1, $"Task {index}", (WellnessCategory)index))
            .ToArray();
        var selector = new WellnessDaySelector(new Random(3));

        var selected = selector.Select(catalogue);

        Assert.Equal(WellnessDefaults.DailyTaskCount, selected.Count);
        Assert.Equal(selected.Count, selected.Select(task => task.Category).Distinct().Count());
    }

    [Theory]
    [InlineData(0, 0, null)]
    [InlineData(0, 6, 0)]
    [InlineData(1, 6, 17)]
    [InlineData(2, 3, 67)]
    [InlineData(6, 6, 100)]
    public void Score_is_percentage_rounded_to_integer(int completed, int total, int? expected)
    {
        Assert.Equal(expected, WellnessScore.Compute(completed, total));
    }

    [Fact]
    public void Civil_today_uses_household_time_zone()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 7, 12, 22, 30, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 7, 13), WellnessCivilDate.Today(clock));
    }

    private static WellnessSelectableTask[] Tasks(params WellnessCategory[] categories) =>
        categories
            .Select((category, index) => new WellnessSelectableTask(index + 1, $"Task {index}", category))
            .ToArray();

    private sealed class FixedClock(DateTimeOffset utcNow) : Segaris.Shared.Time.IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}

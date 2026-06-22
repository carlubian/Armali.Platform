using Segaris.Api.Modules.Recipes.Contracts;

namespace Segaris.Api.IntegrationTests.Recipes;

internal sealed class WeeklyMenuRequestBuilder
{
    private DateOnly? week = new(2026, 6, 22);
    private string? name = "Weekly menu";
    private string? visibility = "Public";
    private IReadOnlyList<WeeklyMenuSlotRequest> slots = [];

    public static WeeklyMenuRequestBuilder Default() => new();

    public WeeklyMenuRequestBuilder WithWeek(DateOnly? value) { week = value; return this; }
    public WeeklyMenuRequestBuilder WithName(string? value) { name = value; return this; }
    public WeeklyMenuRequestBuilder WithVisibility(string? value) { visibility = value; return this; }
    public WeeklyMenuRequestBuilder WithSlots(params WeeklyMenuSlotRequest[] values) { slots = values; return this; }

    public CreateWeeklyMenuRequest BuildCreate() => new(week, name, visibility, slots);

    public UpdateWeeklyMenuRequest BuildUpdate() => new(week, name, visibility, slots);
}

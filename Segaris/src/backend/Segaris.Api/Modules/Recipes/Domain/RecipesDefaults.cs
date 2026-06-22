using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Recipes.Domain;

/// <summary>Frozen validation bounds and creation defaults for the Recipes module.</summary>
internal static class RecipesDefaults
{
    public const int NameMaximumLength = 200;
    public const int NotesMaximumLength = 2000;

    public const int IngredientNameMaximumLength = 200;
    public const int IngredientQuantityMaximumLength = 100;
    public const int StepInstructionMaximumLength = 1000;

    public const int MenuNameMaximumLength = 200;
    public const int CategoryNameMaximumLength = 200;

    /// <summary>The four fixed meal slots that compose every day of a menu grid.</summary>
    public static readonly IReadOnlyList<MealSlot> MealSlots =
    [
        MealSlot.Breakfast,
        MealSlot.Lunch,
        MealSlot.Snack,
        MealSlot.Dinner,
    ];

    /// <summary>
    /// The seven days of a menu grid, anchored on Monday in <c>Europe/Madrid</c>.
    /// </summary>
    public static readonly IReadOnlyList<DayOfWeek> MenuDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday,
    ];

    /// <summary>New recipes and menus default to <see cref="RecordVisibility.Public"/>.</summary>
    public static readonly RecordVisibility Visibility = RecordVisibility.Public;
}

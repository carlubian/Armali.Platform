namespace Segaris.Api.Modules.Recipes.Contracts;

/// <summary>
/// One ordered ingredient line in a recipe create or update. The list is replaced
/// in full and the position is the line's index in the submitted order. The
/// optional <see cref="ItemId"/> links the line to one Inventory item; when absent
/// the line is a complete free-text ingredient.
/// </summary>
internal sealed record RecipeIngredientRequest(
    string? Name,
    string? Quantity,
    int? ItemId);

/// <summary>
/// One ordered preparation step in a recipe create or update. The list is replaced
/// in full and the position is the step's index in the submitted order.
/// </summary>
internal sealed record RecipeStepRequest(string? Instruction);

internal sealed record CreateRecipeRequest(
    string? Name,
    int CategoryId,
    string? Difficulty,
    int? Servings,
    int? PreparationMinutes,
    int? CookMinutes,
    IReadOnlyList<RecipeIngredientRequest> Ingredients,
    IReadOnlyList<RecipeStepRequest> Steps,
    string? Notes,
    string? Visibility);

internal sealed record UpdateRecipeRequest(
    string? Name,
    int CategoryId,
    string? Difficulty,
    int? Servings,
    int? PreparationMinutes,
    int? CookMinutes,
    IReadOnlyList<RecipeIngredientRequest> Ingredients,
    IReadOnlyList<RecipeStepRequest> Steps,
    string? Notes,
    string? Visibility);

/// <summary>
/// One slot of a weekly menu grid in a create or update. <see cref="Day"/> is a
/// <see cref="DayOfWeek"/> name and <see cref="Slot"/> is a <see cref="Domain.MealSlot"/>
/// name; both are validated against the fixed grid. A slot carries zero or more
/// recipe references and no free text.
/// </summary>
internal sealed record WeeklyMenuSlotRequest(
    string? Day,
    string? Slot,
    IReadOnlyList<int> RecipeIds);

internal sealed record CreateWeeklyMenuRequest(
    DateOnly? Week,
    string? Name,
    string? Visibility,
    IReadOnlyList<WeeklyMenuSlotRequest> Slots);

internal sealed record UpdateWeeklyMenuRequest(
    DateOnly? Week,
    string? Name,
    string? Visibility,
    IReadOnlyList<WeeklyMenuSlotRequest> Slots);

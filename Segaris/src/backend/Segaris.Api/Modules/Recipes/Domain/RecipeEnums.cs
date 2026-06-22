namespace Segaris.Api.Modules.Recipes.Domain;

/// <summary>
/// The optional, descriptive recipe difficulty. It is a fixed enum rather than a
/// Configuration catalogue and blocks no operation.
/// </summary>
internal enum RecipeDifficulty
{
    Easy,
    Medium,
    Hard,
}

/// <summary>
/// The four fixed meal slots that make up each day of a weekly menu grid. The set
/// is a fixed enum, not a Configuration catalogue.
/// </summary>
internal enum MealSlot
{
    Breakfast,
    Lunch,
    Snack,
    Dinner,
}

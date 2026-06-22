using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Recipes.Domain;

/// <summary>
/// The Recipes-owned category catalog row. It mirrors the module-owned catalog shape
/// (display name, normalized name for case-insensitive uniqueness, declaration order,
/// and audit metadata) while remaining owned by Recipes and surfaced through
/// Configuration. Because every recipe requires a category, a referenced value may
/// only be replaced; it is never cleared.
/// </summary>
internal sealed class RecipeCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

/// <summary>The editable fields of a recipe, independent of audit metadata.</summary>
internal sealed record RecipeValues(
    string? Name,
    int CategoryId,
    string? Difficulty,
    int? Servings,
    int? PreparationMinutes,
    int? CookMinutes,
    IReadOnlyList<RecipeIngredientValues> Ingredients,
    IReadOnlyList<RecipeStepValues> Steps,
    string? Notes,
    RecordVisibility Visibility);

/// <summary>One ingredient line in a recipe create or update, independent of position.</summary>
internal sealed record RecipeIngredientValues(string? Name, string? Quantity, int? ItemId);

/// <summary>One preparation step in a recipe create or update, independent of position.</summary>
internal sealed record RecipeStepValues(string? Instruction);

/// <summary>
/// A household food recipe. The recipe owns an ordered list of ingredient lines, an
/// ordered list of preparation steps, and standard audit metadata and visibility. The
/// ingredient and step collections are replaced in full on every update; position is
/// assigned by the list index. The category reference is required and points to the
/// module-owned <see cref="RecipeCategory"/> catalog.
/// </summary>
internal sealed class Recipe
{
    private readonly List<RecipeIngredient> ingredients = [];
    private readonly List<RecipeStep> steps = [];

    private Recipe()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public RecipeDifficulty? Difficulty { get; private set; }
    public int? Servings { get; private set; }
    public int? PreparationMinutes { get; private set; }
    public int? CookMinutes { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public int? PrimaryAttachmentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }
    public IReadOnlyList<RecipeIngredient> Ingredients => ingredients;
    public IReadOnlyList<RecipeStep> Steps => steps;

    public static Recipe Create(RecipeValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var recipe = new Recipe
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        recipe.Apply(values, creatorId, now);
        return recipe;
    }

    public void Update(RecipeValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now);
    }

    internal void SetPrimaryAttachment(int? attachmentId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        PrimaryAttachmentId = attachmentId;
        StampModification(actorId, now);
    }

    internal void ClearPrimaryAttachmentIf(int attachmentId, UserId actorId, DateTimeOffset now)
    {
        if (PrimaryAttachmentId != attachmentId)
        {
            return;
        }

        EnsureUtc(now);
        PrimaryAttachmentId = null;
        StampModification(actorId, now);
    }

    /// <summary>
    /// Re-points the category reference to <paramref name="categoryId"/> during a
    /// Configuration category migration. The category is required, so it is replaced
    /// rather than cleared.
    /// </summary>
    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (categoryId <= 0)
        {
            throw new RecipesValidationException("Category identifier must be positive.");
        }

        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    internal void ClearIngredientItemReference(int itemId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);

        var cleared = false;
        foreach (var ingredient in ingredients.Where(ingredient => ingredient.ItemId == itemId))
        {
            ingredient.ClearItemReference();
            cleared = true;
        }

        if (cleared)
        {
            StampModification(actorId, now);
        }
    }

    private void Apply(RecipeValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        var name = RecipesValidation.ValidateName(values.Name);
        if (values.CategoryId <= 0)
        {
            throw new RecipesValidationException("A recipe requires a valid category.");
        }

        var difficulty = ParseDifficulty(values.Difficulty);
        var servings = RecipesValidation.ValidateServings(values.Servings);
        var preparationMinutes = RecipesValidation.ValidateMinutes(values.PreparationMinutes, "Preparation time");
        var cookMinutes = RecipesValidation.ValidateMinutes(values.CookMinutes, "Cook time");
        var notes = RecipesValidation.ValidateNotes(values.Notes);
        if (!Enum.IsDefined(values.Visibility))
        {
            throw new RecipesValidationException("Visibility is invalid.");
        }

        ReplaceIngredients(values.Ingredients);
        ReplaceSteps(values.Steps);

        Name = name;
        CategoryId = values.CategoryId;
        Difficulty = difficulty;
        Servings = servings;
        PreparationMinutes = preparationMinutes;
        CookMinutes = cookMinutes;
        Notes = notes;
        Visibility = values.Visibility;
        StampModification(actorId, now);
    }

    private void ReplaceIngredients(IReadOnlyList<RecipeIngredientValues> ingredientValues)
    {
        ArgumentNullException.ThrowIfNull(ingredientValues);
        if (ingredientValues.Count > RecipesValidation.MaximumIngredients)
        {
            throw new RecipesValidationException(
                $"A recipe may contain at most {RecipesValidation.MaximumIngredients} ingredients.");
        }

        ingredients.Clear();
        for (var index = 0; index < ingredientValues.Count; index++)
        {
            ingredients.Add(RecipeIngredient.Create(ingredientValues[index], position: index));
        }
    }

    private void ReplaceSteps(IReadOnlyList<RecipeStepValues> stepValues)
    {
        ArgumentNullException.ThrowIfNull(stepValues);
        if (stepValues.Count > RecipesValidation.MaximumSteps)
        {
            throw new RecipesValidationException(
                $"A recipe may contain at most {RecipesValidation.MaximumSteps} steps.");
        }

        steps.Clear();
        for (var index = 0; index < stepValues.Count; index++)
        {
            steps.Add(RecipeStep.Create(stepValues[index], position: index));
        }
    }

    private static RecipeDifficulty? ParseDifficulty(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (!Enum.TryParse<RecipeDifficulty>(value, ignoreCase: true, out var difficulty) || !Enum.IsDefined(difficulty))
        {
            throw new RecipesValidationException($"Difficulty '{value}' is not valid.");
        }

        return difficulty;
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new RecipesValidationException("Technical timestamps must use UTC.");
        }
    }
}

/// <summary>
/// A single ordered ingredient line subordinate to exactly one recipe. Position is
/// assigned by the line's index in the submitted collection and is always
/// non-negative. The optional <see cref="ItemId"/> stores an opaque Inventory item
/// reference; its integrity is maintained by the deletion reference contract rather
/// than a foreign key constraint.
/// </summary>
internal sealed class RecipeIngredient
{
    private RecipeIngredient()
    {
    }

    public int Id { get; private set; }
    public int RecipeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Quantity { get; private set; }
    public int? ItemId { get; private set; }
    public int Position { get; private set; }

    internal static RecipeIngredient Create(RecipeIngredientValues values, int position)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (position < 0)
        {
            throw new RecipesValidationException("Ingredient position must be non-negative.");
        }

        return new RecipeIngredient
        {
            Name = RecipesValidation.ValidateIngredientName(values.Name),
            Quantity = RecipesValidation.ValidateIngredientQuantity(values.Quantity),
            ItemId = values.ItemId is { } itemId && itemId > 0 ? itemId : null,
            Position = position,
        };
    }

    /// <summary>
    /// Clears the Inventory item reference when the referenced item is deleted. The
    /// ingredient line itself is preserved with its free-text name.
    /// </summary>
    internal void ClearItemReference()
    {
        ItemId = null;
    }
}

/// <summary>
/// A single ordered preparation step subordinate to exactly one recipe. Position is
/// assigned by the step's index in the submitted collection and is always
/// non-negative.
/// </summary>
internal sealed class RecipeStep
{
    private RecipeStep()
    {
    }

    public int Id { get; private set; }
    public int RecipeId { get; private set; }
    public string Instruction { get; private set; } = string.Empty;
    public int Position { get; private set; }

    internal static RecipeStep Create(RecipeStepValues values, int position)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (position < 0)
        {
            throw new RecipesValidationException("Step position must be non-negative.");
        }

        return new RecipeStep
        {
            Instruction = RecipesValidation.ValidateStepInstruction(values.Instruction),
            Position = position,
        };
    }
}

/// <summary>The editable fields of a weekly menu, independent of audit metadata.</summary>
internal sealed record WeeklyMenuValues(
    DateOnly? Week,
    string? Name,
    RecordVisibility Visibility,
    IReadOnlyList<WeeklyMenuSlotValues> Slots);

/// <summary>One slot assignment in a weekly menu, independent of position within the slot.</summary>
internal sealed record WeeklyMenuSlotValues(string? Day, string? Slot, IReadOnlyList<int> RecipeIds);

/// <summary>
/// A Monday-anchored weekly menu composed of a seven-day by four-slot grid of recipe
/// references. The menu week is always normalised to the Monday of the requested
/// week. Multiple menus for the same week are allowed; visibility and standard
/// authorization govern access. All slot references are removed when a referenced
/// recipe is deleted.
/// </summary>
internal sealed class WeeklyMenu
{
    private readonly List<WeeklyMenuSlotRecipe> slotRecipes = [];

    private WeeklyMenu()
    {
    }

    public int Id { get; private set; }
    public DateOnly Week { get; private set; }
    public string? Name { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }
    public IReadOnlyList<WeeklyMenuSlotRecipe> SlotRecipes => slotRecipes;

    public static WeeklyMenu Create(WeeklyMenuValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var menu = new WeeklyMenu
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        menu.Apply(values, creatorId, now);
        return menu;
    }

    public void Update(WeeklyMenuValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now);
    }

    /// <summary>
    /// Removes all slot references to <paramref name="recipeId"/> when the referenced
    /// recipe is deleted. The slot grid itself is preserved with remaining references.
    /// </summary>
    internal void RemoveRecipeSlots(int recipeId)
    {
        slotRecipes.RemoveAll(slot => slot.RecipeId == recipeId);
    }

    /// <summary>
    /// Normalises <paramref name="date"/> to the Monday of its ISO week. The formula
    /// is correct for all seven weekdays including when <paramref name="date"/> is
    /// already a Monday.
    /// </summary>
    public static DateOnly NormalizeWeek(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    private void Apply(WeeklyMenuValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        if (values.Week is null)
        {
            throw new RecipesValidationException("A weekly menu requires a week date.");
        }

        if (!Enum.IsDefined(values.Visibility))
        {
            throw new RecipesValidationException("Visibility is invalid.");
        }

        var week = NormalizeWeek(values.Week.Value);
        var name = RecipesValidation.ValidateMenuName(values.Name);
        var slots = ParseSlots(values.Slots);

        Week = week;
        Name = name;
        Visibility = values.Visibility;
        ReplaceSlots(slots);
        StampModification(actorId, now);
    }

    private static IReadOnlyList<(DayOfWeek Day, MealSlot Slot, int RecipeId)> ParseSlots(
        IReadOnlyList<WeeklyMenuSlotValues> slotValues)
    {
        ArgumentNullException.ThrowIfNull(slotValues);
        var result = new List<(DayOfWeek, MealSlot, int)>(slotValues.Count);
        foreach (var slotValue in slotValues)
        {
            if (!Enum.TryParse<DayOfWeek>(slotValue.Day, ignoreCase: true, out var day)
                || !RecipesDefaults.MenuDays.Contains(day))
            {
                throw new RecipesValidationException($"Day '{slotValue.Day}' is not a valid menu day.");
            }

            if (!Enum.TryParse<MealSlot>(slotValue.Slot, ignoreCase: true, out var slot)
                || !Enum.IsDefined(slot))
            {
                throw new RecipesValidationException($"Slot '{slotValue.Slot}' is not a valid meal slot.");
            }

            ArgumentNullException.ThrowIfNull(slotValue.RecipeIds);
            foreach (var recipeId in slotValue.RecipeIds)
            {
                if (recipeId <= 0)
                {
                    throw new RecipesValidationException("Recipe identifiers in a slot must be positive.");
                }

                result.Add((day, slot, recipeId));
            }
        }

        return result;
    }

    private void ReplaceSlots(IReadOnlyList<(DayOfWeek Day, MealSlot Slot, int RecipeId)> slots)
    {
        slotRecipes.Clear();
        var seen = new HashSet<(DayOfWeek, MealSlot, int)>();
        foreach (var (day, slot, recipeId) in slots)
        {
            if (!seen.Add((day, slot, recipeId)))
            {
                throw new RecipesValidationException(
                    $"Recipe {recipeId} appears more than once in the {day}/{slot} slot.");
            }

            slotRecipes.Add(new WeeklyMenuSlotRecipe { Day = day, Slot = slot, RecipeId = recipeId });
        }
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new RecipesValidationException("Technical timestamps must use UTC.");
        }
    }
}

/// <summary>
/// A single recipe reference in a weekly menu slot. The composite key
/// <c>(MenuId, Day, Slot, RecipeId)</c> prevents the same recipe appearing more than
/// once in the same slot while allowing multiple different recipes per slot. The
/// reference uses a normal relationship to <see cref="Recipe"/>; the menu removes the
/// slot entry rather than leaving a dangling reference when the recipe is deleted.
/// </summary>
internal sealed class WeeklyMenuSlotRecipe
{
    public int MenuId { get; set; }
    public DayOfWeek Day { get; set; }
    public MealSlot Slot { get; set; }
    public int RecipeId { get; set; }
}

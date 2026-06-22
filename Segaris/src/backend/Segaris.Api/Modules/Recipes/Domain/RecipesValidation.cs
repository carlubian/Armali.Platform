namespace Segaris.Api.Modules.Recipes.Domain;

/// <summary>Domain limits shared across the Recipes module Waves.</summary>
internal static class RecipesValidation
{
    public const int MaximumIngredients = 200;
    public const int MaximumSteps = 100;
    public const int MinimumServings = 1;
    public const int MaximumServings = 100;
    public const int MinimumMinutes = 1;
    public const int MaximumMinutes = 1440;

    public static string ValidateName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > RecipesDefaults.NameMaximumLength)
        {
            throw new RecipesValidationException(
                $"Name is required and may contain at most {RecipesDefaults.NameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string? ValidateNotes(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length > RecipesDefaults.NotesMaximumLength)
        {
            throw new RecipesValidationException(
                $"Notes may contain at most {RecipesDefaults.NotesMaximumLength} characters.");
        }

        return value.Length == 0 ? null : value;
    }

    public static string ValidateIngredientName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > RecipesDefaults.IngredientNameMaximumLength)
        {
            throw new RecipesValidationException(
                $"Ingredient name is required and may contain at most {RecipesDefaults.IngredientNameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string? ValidateIngredientQuantity(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > RecipesDefaults.IngredientQuantityMaximumLength)
        {
            throw new RecipesValidationException(
                $"Ingredient quantity may contain at most {RecipesDefaults.IngredientQuantityMaximumLength} characters.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string ValidateStepInstruction(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > RecipesDefaults.StepInstructionMaximumLength)
        {
            throw new RecipesValidationException(
                $"Step instruction is required and may contain at most {RecipesDefaults.StepInstructionMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string ValidateCategoryName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > RecipesDefaults.CategoryNameMaximumLength)
        {
            throw new RecipesValidationException(
                $"Category name is required and may contain at most {RecipesDefaults.CategoryNameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string? ValidateMenuName(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > RecipesDefaults.MenuNameMaximumLength)
        {
            throw new RecipesValidationException(
                $"Menu name may contain at most {RecipesDefaults.MenuNameMaximumLength} characters.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    public static int? ValidateServings(int? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value < MinimumServings || value > MaximumServings)
        {
            throw new RecipesValidationException(
                $"Servings must be between {MinimumServings} and {MaximumServings}.");
        }

        return value;
    }

    public static int? ValidateMinutes(int? value, string field)
    {
        if (value is null)
        {
            return null;
        }

        if (value < MinimumMinutes || value > MaximumMinutes)
        {
            throw new RecipesValidationException(
                $"{field} must be between {MinimumMinutes} and {MaximumMinutes}.");
        }

        return value;
    }
}

/// <summary>
/// Distinguishes the Recipes domain failures so the HTTP surface can map each one
/// to its frozen <see cref="RecipesErrorCodes"/> value.
/// </summary>
internal enum RecipesValidationReason
{
    /// <summary>A required string, length, enum, or bound rule failed.</summary>
    Validation,

    /// <summary>A referenced category does not exist or has become invalid.</summary>
    CatalogReference,

    /// <summary>A visibility change would violate ownership or privacy rules.</summary>
    VisibilityForbidden,

    /// <summary>A referenced Inventory item does not exist or is not accessible.</summary>
    IngredientItemNotAccessible,

    /// <summary>An ingredient item reference would violate recipe visibility.</summary>
    IngredientItemVisibilityForbidden,
}

internal sealed class RecipesValidationException(
    string message,
    RecipesValidationReason reason = RecipesValidationReason.Validation) : Exception(message)
{
    public RecipesValidationReason Reason { get; } = reason;
}

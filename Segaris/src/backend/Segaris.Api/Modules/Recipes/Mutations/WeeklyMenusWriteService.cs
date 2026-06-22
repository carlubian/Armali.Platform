using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Recipes.Mutations;

/// <summary>
/// Write-side operations on weekly menus. Menu references are validated against
/// recipe accessibility and the menu-to-recipe visibility rule before persistence.
/// </summary>
internal sealed class WeeklyMenusWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        CreateWeeklyMenuRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(request.Week, request.Name, request.Visibility, request.Slots);
        var menu = WeeklyMenu.Create(values, actorId, clock.UtcNow);
        await ValidateRecipeReferencesAsync(values, actorId, cancellationToken);

        database.Add(menu);
        await database.SaveChangesAsync(cancellationToken);
        return menu.Id;
    }

    public async Task<bool> UpdateAsync(
        int menuId,
        UpdateWeeklyMenuRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var menu = await database.Set<WeeklyMenu>()
            .Where(WeeklyMenuPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == menuId)
            .Include(candidate => candidate.SlotRecipes)
            .FirstOrDefaultAsync(cancellationToken);
        if (menu is null)
        {
            return false;
        }

        var values = Map(request.Week, request.Name, request.Visibility, request.Slots);
        ValidateVisibilityChange(menu, values.Visibility, actorId);
        menu.Update(values, actorId, clock.UtcNow);
        await ValidateRecipeReferencesAsync(values, actorId, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int menuId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var menu = await database.Set<WeeklyMenu>()
            .Where(WeeklyMenuPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == menuId)
            .FirstOrDefaultAsync(cancellationToken);
        if (menu is null)
        {
            return false;
        }

        database.Remove(menu);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateRecipeReferencesAsync(
        WeeklyMenuValues values,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var recipeIds = values.Slots
            .SelectMany(slot => slot.RecipeIds)
            .Distinct()
            .ToArray();
        if (recipeIds.Length == 0)
        {
            return;
        }

        var referencedRecipes = await database.Set<Recipe>()
            .AsNoTracking()
            .Where(RecipePolicies.AccessibleTo(actorId))
            .Where(recipe => recipeIds.Contains(recipe.Id))
            .Select(recipe => new { recipe.Id, recipe.Visibility })
            .ToArrayAsync(cancellationToken);

        if (referencedRecipes.Length != recipeIds.Length)
        {
            throw new RecipesValidationException(
                "One or more menu recipes were not found.",
                RecipesValidationReason.MenuRecipeNotAccessible);
        }

        if (values.Visibility == RecordVisibility.Public
            && referencedRecipes.Any(recipe => recipe.Visibility != RecordVisibility.Public))
        {
            throw new RecipesValidationException(
                "A public menu may reference only public recipes.",
                RecipesValidationReason.MenuRecipeVisibilityForbidden);
        }
    }

    private static void ValidateVisibilityChange(
        WeeklyMenu menu,
        RecordVisibility requestedVisibility,
        UserId actorId)
    {
        if (requestedVisibility != menu.Visibility
            && !WeeklyMenuPolicies.CanChangeVisibility(menu, actorId))
        {
            throw new RecipesValidationException(
                "Only the creator may change menu visibility.",
                RecipesValidationReason.VisibilityForbidden);
        }
    }

    private static WeeklyMenuValues Map(
        DateOnly? week,
        string? name,
        string? visibility,
        IReadOnlyList<WeeklyMenuSlotRequest>? slots) => new(
            week,
            name,
            ParseEnum(visibility, RecipesDefaults.Visibility, "visibility"),
            (slots ?? []).Select(slot => new WeeklyMenuSlotValues(slot.Day, slot.Slot, slot.RecipeIds ?? [])).ToArray());

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new RecipesValidationException($"The {field} is not a recognized value.");
    }
}

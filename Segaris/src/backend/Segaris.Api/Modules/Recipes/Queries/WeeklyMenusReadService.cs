using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Recipes.Queries;

/// <summary>
/// Read-side queries for weekly menus. Every menu query filters to records
/// accessible to the viewer before projecting menu metadata or slot contents.
/// </summary>
internal sealed class WeeklyMenusReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<WeeklyMenuSummaryResponse>> ListAsync(
        WeeklyMenuFilter filter,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var menus = database.Set<WeeklyMenu>()
            .AsNoTracking()
            .Where(WeeklyMenuPolicies.AccessibleTo(userId));

        if (filter.Week is { } week)
        {
            menus = menus.Where(menu => menu.Week == week);
        }

        return await menus
            .OrderByDescending(menu => menu.Week)
            .ThenBy(menu => menu.Name)
            .ThenBy(menu => menu.Id)
            .Select(menu => new WeeklyMenuSummaryResponse(
                menu.Id,
                menu.Week,
                menu.Name,
                menu.Visibility.ToString(),
                menu.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == menu.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<WeeklyMenuResponse?> GetAsync(
        int menuId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<WeeklyMenu>()
            .AsNoTracking()
            .Where(WeeklyMenuPolicies.AccessibleTo(userId))
            .Where(menu => menu.Id == menuId)
            .Select(menu => new WeeklyMenuDetailRow(
                menu.Id,
                menu.Week,
                menu.Name,
                menu.Visibility,
                menu.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == menu.CreatedBy).Select(user => user.DisplayName).First(),
                menu.CreatedAt,
                menu.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == menu.UpdatedBy).Select(user => user.DisplayName).First(),
                menu.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var slotRows = await database.Set<WeeklyMenuSlotRecipe>()
            .AsNoTracking()
            .Where(slot => slot.MenuId == row.Id)
            .OrderBy(slot => slot.Day)
            .ThenBy(slot => slot.Slot)
            .ThenBy(slot => slot.RecipeId)
            .Select(slot => new WeeklyMenuSlotRow(slot.Day, slot.Slot, slot.RecipeId))
            .ToArrayAsync(cancellationToken);

        var recipeIds = slotRows.Select(slot => slot.RecipeId).Distinct().ToArray();
        var recipes = await database.Set<Recipe>()
            .AsNoTracking()
            .Where(RecipePolicies.AccessibleTo(userId))
            .Where(recipe => recipeIds.Contains(recipe.Id))
            .Select(recipe => new WeeklyMenuRecipeRow(recipe.Id, recipe.Name))
            .ToDictionaryAsync(recipe => recipe.Id, cancellationToken);

        var slots = ProjectGrid(slotRows, recipes);

        return new WeeklyMenuResponse(
            row.Id,
            row.Week,
            row.Name,
            row.Visibility.ToString(),
            slots,
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private static IReadOnlyList<WeeklyMenuSlotResponse> ProjectGrid(
        IReadOnlyList<WeeklyMenuSlotRow> slotRows,
        IReadOnlyDictionary<int, WeeklyMenuRecipeRow> recipes)
    {
        var grouped = slotRows
            .GroupBy(slot => (slot.Day, slot.Slot))
            .ToDictionary(group => group.Key, group => group.OrderBy(slot => slot.RecipeId).ToArray());
        var grid = new List<WeeklyMenuSlotResponse>(RecipesDefaults.MenuDays.Count * RecipesDefaults.MealSlots.Count);

        foreach (var day in RecipesDefaults.MenuDays)
        {
            foreach (var slot in RecipesDefaults.MealSlots)
            {
                grouped.TryGetValue((day, slot), out var rows);
                var slotRecipes = (rows ?? Array.Empty<WeeklyMenuSlotRow>())
                    .Select(row => recipes.TryGetValue(row.RecipeId, out var recipe)
                        ? new WeeklyMenuSlotRecipeResponse(row.RecipeId, recipe.Name, PlaceholderThumbnail())
                        : new WeeklyMenuSlotRecipeResponse(row.RecipeId, null, PlaceholderThumbnail()))
                    .ToArray();
                grid.Add(new WeeklyMenuSlotResponse(day.ToString(), slot.ToString(), slotRecipes));
            }
        }

        return grid;
    }

    private static RecipeThumbnailResponse PlaceholderThumbnail() => new(null, null, "placeholder");

    private sealed record WeeklyMenuDetailRow(
        int Id,
        DateOnly Week,
        string? Name,
        RecordVisibility Visibility,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);

    private sealed record WeeklyMenuSlotRow(DayOfWeek Day, MealSlot Slot, int RecipeId);

    private sealed record WeeklyMenuRecipeRow(int Id, string Name);
}

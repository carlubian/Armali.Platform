using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Recipes.Mutations;

/// <summary>
/// Recipes implementation of the Inventory-owned item deletion-reference contract.
/// Inventory enumerates this handler through DI and never references Recipes entities
/// directly.
/// </summary>
internal sealed class RecipesInventoryItemDeletionReferenceHandler(SegarisDbContext database)
    : IInventoryItemDeletionReferenceHandler
{
    public Task<int> CountReferencesAsync(int itemId, CancellationToken cancellationToken) =>
        database.Set<RecipeIngredient>()
            .AsNoTracking()
            .CountAsync(ingredient => ingredient.ItemId == itemId, cancellationToken);

    public async Task ClearReferencesAsync(
        InventoryItemDeletionClearing clearing,
        CancellationToken cancellationToken)
    {
        var recipes = await database.Set<Recipe>()
            .Where(recipe => recipe.Ingredients.Any(ingredient => ingredient.ItemId == clearing.ItemId))
            .Include(recipe => recipe.Ingredients)
            .ToListAsync(cancellationToken);

        foreach (var recipe in recipes)
        {
            recipe.ClearIngredientItemReference(clearing.ItemId, clearing.Actor, clearing.OccurredAt);
        }
    }
}

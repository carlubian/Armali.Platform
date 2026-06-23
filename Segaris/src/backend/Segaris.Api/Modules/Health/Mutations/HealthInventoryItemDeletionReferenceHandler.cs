using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Health.Mutations;

/// <summary>
/// Health implementation of the Inventory-owned item deletion-reference contract.
/// Inventory enumerates this handler through DI and never references Health entities
/// directly.
/// </summary>
internal sealed class HealthInventoryItemDeletionReferenceHandler(SegarisDbContext database)
    : IInventoryItemDeletionReferenceHandler
{
    public Task<int> CountReferencesAsync(int itemId, CancellationToken cancellationToken) =>
        database.Set<Medicine>()
            .AsNoTracking()
            .CountAsync(medicine => medicine.InventoryItemId == itemId, cancellationToken);

    public async Task ClearReferencesAsync(
        InventoryItemDeletionClearing clearing,
        CancellationToken cancellationToken)
    {
        var medicines = await database.Set<Medicine>()
            .Where(medicine => medicine.InventoryItemId == clearing.ItemId)
            .ToListAsync(cancellationToken);

        foreach (var medicine in medicines)
        {
            medicine.ClearInventoryItemReference(clearing.ItemId, clearing.Actor, clearing.OccurredAt);
        }
    }
}

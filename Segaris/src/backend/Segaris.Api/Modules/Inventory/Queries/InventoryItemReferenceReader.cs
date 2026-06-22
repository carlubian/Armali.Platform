using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Queries;

internal sealed class InventoryItemReferenceReader(SegarisDbContext database) : IInventoryItemReferenceReader
{
    public Task<InventoryItemReference?> FindAccessibleAsync(
        int itemId,
        UserId viewer,
        CancellationToken cancellationToken) =>
        database.Set<InventoryItem>()
            .AsNoTracking()
            .Where(InventoryItemPolicies.AccessibleTo(viewer))
            .Where(item => item.Id == itemId)
            .Select(item => new InventoryItemReference(item.Id, item.Name, item.Visibility))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, InventoryItemReference>> ResolveAccessibleAsync(
        IReadOnlyCollection<int> itemIds,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return new Dictionary<int, InventoryItemReference>();
        }

        return await database.Set<InventoryItem>()
            .AsNoTracking()
            .Where(InventoryItemPolicies.AccessibleTo(viewer))
            .Where(item => itemIds.Contains(item.Id))
            .Select(item => new InventoryItemReference(item.Id, item.Name, item.Visibility))
            .ToDictionaryAsync(item => item.ItemId, cancellationToken);
    }
}

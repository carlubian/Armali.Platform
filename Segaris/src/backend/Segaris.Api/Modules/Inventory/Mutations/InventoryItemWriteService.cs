using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Inventory.Mutations;

/// <summary>
/// Write-side operations on Inventory items. Wave 2 implements only the quick stock
/// adjustment, which updates the item's current stock and modification metadata
/// without any stock-movement record. Authorization follows the item visibility
/// rules: an inaccessible item is reported as not found so a private item is never
/// disclosed. Later Waves extend this service with full item create, update, and
/// delete.
/// </summary>
internal sealed class InventoryItemWriteService(SegarisDbContext database, IClock clock)
{
    /// <summary>
    /// Applies a quick stock increase or decrease to an accessible item. Returns
    /// <c>false</c> when the item does not exist or is not accessible. Throws
    /// <see cref="InventoryValidationException"/> when the direction or quantity is
    /// invalid or the result would be negative.
    /// </summary>
    public async Task<bool> AdjustStockAsync(
        int itemId,
        InventoryStockAdjustmentRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var direction = ParseDirection(request.Direction);

        var item = await database.Set<InventoryItem>()
            .Where(InventoryItemPolicies.AccessibleTo(actorId))
            .Where(candidate => candidate.Id == itemId)
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return false;
        }

        item.AdjustStock(direction, request.Quantity, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static InventoryStockAdjustmentDirection ParseDirection(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<InventoryStockAdjustmentDirection>(value, ignoreCase: false, out var direction)
            && Enum.IsDefined(direction))
        {
            return direction;
        }

        throw new InventoryValidationException(
            "The stock adjustment direction must be 'Increase' or 'Decrease'.");
    }
}

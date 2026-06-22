using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Inventory.Mutations;

/// <summary>
/// Write-side operations on Inventory items. Wave 2 introduced the quick stock
/// adjustment, which updates the item's current stock and modification metadata
/// without any stock-movement record. Wave 3 adds full item create, update,
/// delete, and attachment cleanup support. Authorization follows the item
/// visibility rules: an inaccessible item is reported as not found so a private
/// item is never disclosed.
/// </summary>
internal sealed class InventoryItemWriteService(
    SegarisDbContext database,
    IAttachmentService attachments,
    IEnumerable<IInventoryItemDeletionReferenceHandler> deletionReferenceHandlers,
    IClock clock)
{
    private readonly IReadOnlyList<IInventoryItemDeletionReferenceHandler> deletionReferenceHandlers =
        deletionReferenceHandlers.ToArray();

    public async Task<int> CreateAsync(
        CreateInventoryItemRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(
            request.Name,
            request.Status,
            request.Notes,
            request.CategoryId,
            request.LocationId,
            request.CurrentStock,
            request.MinimumStock,
            request.SupplierIds,
            request.Visibility);

        var item = InventoryItem.Create(values, actorId, clock.UtcNow);
        await ValidateReferencesAsync(values, cancellationToken);

        database.Add(item);
        await database.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<bool> UpdateAsync(
        int itemId,
        UpdateInventoryItemRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await database.Set<InventoryItem>()
            .Where(InventoryItemPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == itemId)
            .Include(candidate => candidate.Suppliers)
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return false;
        }

        var values = Map(
            request.Name,
            request.Status,
            request.Notes,
            request.CategoryId,
            request.LocationId,
            request.CurrentStock,
            request.MinimumStock,
            request.SupplierIds,
            request.Visibility);

        await ValidateItemVisibilityChangeAsync(item, values.Visibility, actorId, cancellationToken);
        item.Update(values, actorId, clock.UtcNow);
        await ValidateReferencesAsync(values, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int itemId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var item = await database.Set<InventoryItem>()
            .Where(InventoryItemPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == itemId)
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return false;
        }

        if (await database.Set<InventoryOrderLine>().AnyAsync(line => line.ItemId == itemId, cancellationToken))
        {
            throw new InventoryValidationException(
                "An item referenced by an order cannot be deleted.",
                InventoryValidationReason.ReferencedByOrder);
        }

        var clearing = new InventoryItemDeletionClearing(itemId, actorId, clock.UtcNow);
        foreach (var handler in deletionReferenceHandlers)
        {
            await handler.ClearReferencesAsync(clearing, cancellationToken);
        }

        database.Remove(item);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var owner = InventoryAttachments.ItemOwner(itemId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }

        return true;
    }

    public async Task<InventoryItemDeletionImpactResponse?> GetDeletionImpactAsync(
        int itemId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var itemExists = await database.Set<InventoryItem>()
            .AsNoTracking()
            .Where(InventoryItemPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == itemId)
            .AnyAsync(cancellationToken);
        if (!itemExists)
        {
            return null;
        }

        var references = await CountDeletionReferencesAsync(itemId, cancellationToken);
        return new(references > 0, references);
    }

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

    private async Task ValidateReferencesAsync(
        InventoryItemValues values,
        CancellationToken cancellationToken)
    {
        var categoryExists = await database.Set<InventoryCategory>()
            .AnyAsync(category => category.Id == values.CategoryId, cancellationToken);
        var locationExists = await database.Set<InventoryLocation>()
            .AnyAsync(location => location.Id == values.LocationId, cancellationToken);
        var distinctSupplierIds = values.SupplierIds.Distinct().ToArray();
        var supplierCount = await database.Set<SegarisSupplier>()
            .CountAsync(supplier => distinctSupplierIds.Contains(supplier.Id), cancellationToken);

        if (!categoryExists || !locationExists || supplierCount != distinctSupplierIds.Length)
        {
            throw new InventoryValidationException(
                "One or more Inventory item catalog references do not exist.",
                InventoryValidationReason.CatalogReference);
        }
    }

    private async Task<int> CountDeletionReferencesAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var handler in deletionReferenceHandlers)
        {
            count += await handler.CountReferencesAsync(itemId, cancellationToken);
        }

        return count;
    }

    private async Task ValidateItemVisibilityChangeAsync(
        InventoryItem item,
        RecordVisibility requestedVisibility,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        if (requestedVisibility == item.Visibility)
        {
            return;
        }

        if (!InventoryItemPolicies.CanChangeVisibility(item, actorId))
        {
            throw new InventoryValidationException(
                "Only the creator may change item visibility.",
                InventoryValidationReason.VisibilityForbidden);
        }

        if (item.Visibility == RecordVisibility.Public
            && requestedVisibility == RecordVisibility.Private
            && await database.Set<InventoryOrderLine>()
                .Where(line => line.ItemId == item.Id)
                .AnyAsync(line => database.Set<InventoryOrder>()
                    .Any(order => order.Id == line.OrderId && order.Visibility == RecordVisibility.Public), cancellationToken))
        {
            throw new InventoryValidationException(
                "An item used by a public order cannot be made private.",
                InventoryValidationReason.VisibilityForbidden);
        }
    }

    private static InventoryItemValues Map(
        string? name,
        string? status,
        string? notes,
        int categoryId,
        int locationId,
        decimal currentStock,
        decimal minimumStock,
        IReadOnlyList<int>? supplierIds,
        string? visibility) => new(
            name ?? string.Empty,
            ParseEnum(status, InventoryDefaults.ItemStatus, "status"),
            notes,
            categoryId,
            locationId,
            currentStock,
            minimumStock,
            supplierIds ?? [],
            ParseEnum(visibility, InventoryDefaults.Visibility, "visibility"));

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

        throw new InventoryValidationException($"The {field} is not a recognized value.");
    }
}

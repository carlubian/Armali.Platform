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

internal enum InventoryOrderValidationReason
{
    Validation,
    CatalogReference,
    ReceivedLocked,
    VisibilityForbidden,
    LineSupplierNotAllowed,
    LineItemNotAccessible,
}

internal sealed class InventoryOrderValidationException(
    string message,
    InventoryOrderValidationReason reason = InventoryOrderValidationReason.Validation) : Exception(message)
{
    public InventoryOrderValidationReason Reason { get; } = reason;
}

/// <summary>
/// Write-side operations on Inventory orders. Stock movement is deliberately absent
/// here; Wave 5 owns the explicit receive transaction.
/// </summary>
internal sealed class InventoryOrderWriteService(
    SegarisDbContext database,
    IAttachmentService attachments,
    IClock clock)
{
    public async Task<int> CreateAsync(
        CreateInventoryOrderRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = MapCreate(request);
        var order = CreateOrder(values, actorId);
        await ValidateReferencesAndLinesAsync(values, actorId, cancellationToken);

        database.Add(order);
        await database.SaveChangesAsync(cancellationToken);
        return order.Id;
    }

    public async Task<bool> UpdateAsync(
        int orderId,
        UpdateInventoryOrderRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await database.Set<InventoryOrder>()
            .Where(InventoryOrderPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == orderId)
            .Include(candidate => candidate.Lines)
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return false;
        }

        var values = MapUpdate(request);
        ValidateReceivedOrderUpdate(order, values);
        await ValidateOrderVisibilityChangeAsync(order, values, actorId, cancellationToken);
        await ValidateReferencesAndLinesAsync(values, actorId, cancellationToken);

        try
        {
            order.Update(values, actorId, clock.UtcNow);
        }
        catch (InventoryValidationException exception)
        {
            throw new InventoryOrderValidationException(exception.Message);
        }

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int orderId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var order = await database.Set<InventoryOrder>()
            .Where(InventoryOrderPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == orderId)
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return false;
        }

        database.Remove(order);
        await database.SaveChangesAsync(cancellationToken);

        var owner = InventoryAttachments.OrderOwner(orderId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }

        return true;
    }

    private InventoryOrder CreateOrder(InventoryOrderValues values, UserId actorId)
    {
        try
        {
            return InventoryOrder.Create(values, actorId, clock.UtcNow);
        }
        catch (InventoryValidationException exception)
        {
            throw new InventoryOrderValidationException(exception.Message);
        }
    }

    private async Task ValidateReferencesAndLinesAsync(
        InventoryOrderValues values,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var supplierExists = await database.Set<SegarisSupplier>()
            .AnyAsync(supplier => supplier.Id == values.SupplierId, cancellationToken);
        var currencyExists = await database.Set<SegarisCurrency>()
            .AnyAsync(currency => currency.Id == values.CurrencyId, cancellationToken);
        if (!supplierExists || !currencyExists)
        {
            throw new InventoryOrderValidationException(
                "One or more Inventory order catalog references do not exist.",
                InventoryOrderValidationReason.CatalogReference);
        }

        var itemIds = values.Lines.Select(line => line.ItemId).Distinct().ToArray();
        var accessibleItems = await database.Set<InventoryItem>()
            .Where(InventoryItemPolicies.AccessibleTo(actorId))
            .Where(item => itemIds.Contains(item.Id))
            .Select(item => new
            {
                item.Id,
                item.Visibility,
                SupplierAllowed = item.Suppliers.Any(supplier => supplier.SupplierId == values.SupplierId),
            })
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        if (accessibleItems.Count != itemIds.Length)
        {
            throw new InventoryOrderValidationException(
                "One or more order-line items were not found.",
                InventoryOrderValidationReason.LineItemNotAccessible);
        }

        if (values.Visibility == RecordVisibility.Public
            && accessibleItems.Values.Any(item => item.Visibility != RecordVisibility.Public))
        {
            throw new InventoryOrderValidationException(
                "A public order may contain only public items.",
                InventoryOrderValidationReason.VisibilityForbidden);
        }

        if (accessibleItems.Values.Any(item => !item.SupplierAllowed))
        {
            throw new InventoryOrderValidationException(
                "Every order line item must allow the order supplier.",
                InventoryOrderValidationReason.LineSupplierNotAllowed);
        }
    }

    private async Task ValidateOrderVisibilityChangeAsync(
        InventoryOrder order,
        InventoryOrderValues values,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        if (values.Visibility == order.Visibility)
        {
            return;
        }

        if (!InventoryOrderPolicies.CanChangeVisibility(order, actorId))
        {
            throw new InventoryOrderValidationException(
                "Only the creator may change order visibility.",
                InventoryOrderValidationReason.VisibilityForbidden);
        }

        if (values.Visibility == RecordVisibility.Public)
        {
            var itemIds = values.Lines.Select(line => line.ItemId).Distinct().ToArray();
            var privateItemExists = await database.Set<InventoryItem>()
                .Where(InventoryItemPolicies.AccessibleTo(actorId))
                .Where(item => itemIds.Contains(item.Id))
                .AnyAsync(item => item.Visibility != RecordVisibility.Public, cancellationToken);
            if (privateItemExists)
            {
                throw new InventoryOrderValidationException(
                    "A private order containing private items cannot be made public.",
                    InventoryOrderValidationReason.VisibilityForbidden);
            }
        }
    }

    private static void ValidateReceivedOrderUpdate(InventoryOrder order, InventoryOrderValues values)
    {
        if (order.Status != InventoryOrderStatus.Received)
        {
            return;
        }

        if (values.Status == InventoryOrderStatus.Received || !OnlyStatusChanges(order, values))
        {
            throw new InventoryOrderValidationException(
                "Received orders are locked until their status is moved back to another state.",
                InventoryOrderValidationReason.ReceivedLocked);
        }
    }

    private static bool OnlyStatusChanges(InventoryOrder order, InventoryOrderValues values) =>
        order.SupplierId == values.SupplierId
        && order.CurrencyId == values.CurrencyId
        && order.OrderDate == values.OrderDate
        && order.ExpectedReceiptDate == values.ExpectedReceiptDate
        && string.Equals(order.Notes, InventoryValidation.ValidateNotes(values.Notes), StringComparison.Ordinal)
        && order.Visibility == values.Visibility
        && LinesMatch(order.Lines, values.Lines);

    private static bool LinesMatch(
        IReadOnlyList<InventoryOrderLine> existing,
        IReadOnlyList<InventoryOrderLineValues> requested)
    {
        if (existing.Count != requested.Count)
        {
            return false;
        }

        var ordered = existing.OrderBy(line => line.Id).ToArray();
        for (var index = 0; index < ordered.Length; index++)
        {
            if (ordered[index].ItemId != requested[index].ItemId
                || ordered[index].Quantity != requested[index].Quantity
                || ordered[index].LineTotal != requested[index].LineTotal)
            {
                return false;
            }
        }

        return true;
    }

    private InventoryOrderValues MapCreate(CreateInventoryOrderRequest request) => new(
        request.SupplierId,
        ParseEnum(request.Status, InventoryDefaults.OrderStatus, "status"),
        request.CurrencyId,
        request.OrderDate ?? InventoryDefaults.OrderDate(clock.UtcNow),
        request.ExpectedReceiptDate ?? InventoryDefaults.ExpectedReceiptDate(clock.UtcNow),
        request.Notes,
        ParseEnum(request.Visibility, InventoryDefaults.Visibility, "visibility"),
        MapLines(request.Lines));

    private static InventoryOrderValues MapUpdate(UpdateInventoryOrderRequest request) => new(
        request.SupplierId,
        ParseEnum(request.Status, InventoryDefaults.OrderStatus, "status"),
        request.CurrencyId,
        request.OrderDate,
        request.ExpectedReceiptDate,
        request.Notes,
        ParseEnum(request.Visibility, InventoryDefaults.Visibility, "visibility"),
        MapLines(request.Lines));

    private static IReadOnlyList<InventoryOrderLineValues> MapLines(IReadOnlyList<InventoryOrderLineRequest>? lines) =>
        lines?.Select(line => new InventoryOrderLineValues(line.ItemId, line.Quantity, line.LineTotal)).ToArray() ?? [];

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

        throw new InventoryOrderValidationException($"The {field} is not a recognized value.");
    }
}

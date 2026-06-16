using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Domain;

/// <summary>
/// An Inventory-owned catalog row (category or location). It mirrors the
/// shared-catalog shape (display name, normalized name for case-insensitive
/// uniqueness, declaration order, and audit metadata) while remaining owned by
/// Inventory and surfaced through Configuration.
/// </summary>
internal sealed class InventoryCategory
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

/// <summary>The Inventory-owned location catalog row. See <see cref="InventoryCategory"/>.</summary>
internal sealed class InventoryLocation
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

/// <summary>The editable fields of an Inventory item, independent of audit metadata.</summary>
internal sealed record InventoryItemValues(
    string Name,
    InventoryItemStatus Status,
    string? Notes,
    int CategoryId,
    int LocationId,
    decimal CurrentStock,
    decimal MinimumStock,
    IReadOnlyList<int> SupplierIds,
    RecordVisibility Visibility);

/// <summary>
/// A stock-tracked household item. The item owns its current and minimum stock, its
/// category and location references, its visibility, and the set of suppliers
/// allowed for future order lines. Stock is an authoritative property of the item
/// itself: there is no separate movement table and stock is never split across
/// locations.
/// </summary>
internal sealed class InventoryItem
{
    private readonly List<InventoryItemSupplier> suppliers = [];

    private InventoryItem()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public InventoryItemStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public int CategoryId { get; private set; }
    public int LocationId { get; private set; }
    public decimal CurrentStock { get; private set; }
    public decimal MinimumStock { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }
    public IReadOnlyList<InventoryItemSupplier> Suppliers => suppliers;

    public static InventoryItem Create(InventoryItemValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var item = new InventoryItem
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        item.Apply(values, creatorId, now);
        return item;
    }

    public void Update(InventoryItemValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now);
    }

    /// <summary>Applies a quick stock adjustment, rejecting a negative result.</summary>
    public void AdjustStock(
        InventoryStockAdjustmentDirection direction,
        decimal quantity,
        UserId actorId,
        DateTimeOffset now)
    {
        EnsureUtc(now);
        if (!Enum.IsDefined(direction))
        {
            throw new InventoryValidationException("The stock adjustment direction is invalid.");
        }

        CurrentStock = InventoryValidation.ApplyStockAdjustment(CurrentStock, direction, quantity);
        StampModification(actorId, now);
    }

    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (categoryId <= 0)
        {
            throw new InventoryValidationException("Catalog identifiers must be positive.");
        }

        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    internal void ReplaceLocation(int locationId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (locationId <= 0)
        {
            throw new InventoryValidationException("Catalog identifiers must be positive.");
        }

        LocationId = locationId;
        StampModification(actorId, now);
    }

    /// <summary>
    /// Re-points the allowed-supplier eligibility from <paramref name="sourceSupplierId"/>
    /// to <paramref name="targetSupplierId"/> during a Configuration supplier migration.
    /// The source association is removed and the target added unless the item already
    /// allows it, so the item never gains a duplicate row and never drops below its
    /// required single supplier. The item is untouched and unstamped when it did not
    /// allow the source supplier.
    /// </summary>
    internal void ReplaceSupplier(int sourceSupplierId, int targetSupplierId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (sourceSupplierId <= 0 || targetSupplierId <= 0)
        {
            throw new InventoryValidationException("Supplier identifiers must be positive.");
        }

        if (suppliers.RemoveAll(association => association.SupplierId == sourceSupplierId) == 0)
        {
            return;
        }

        if (suppliers.All(association => association.SupplierId != targetSupplierId))
        {
            suppliers.Add(new InventoryItemSupplier { SupplierId = targetSupplierId });
        }

        StampModification(actorId, now);
    }

    private void Apply(InventoryItemValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        var name = InventoryValidation.ValidateItemName(values.Name);
        var notes = InventoryValidation.ValidateNotes(values.Notes);
        if (!Enum.IsDefined(values.Status) || !Enum.IsDefined(values.Visibility))
        {
            throw new InventoryValidationException("Status or visibility is invalid.");
        }

        if (values.CategoryId <= 0 || values.LocationId <= 0)
        {
            throw new InventoryValidationException("Catalog identifiers must be positive.");
        }

        var currentStock = InventoryValidation.ValidateStock(values.CurrentStock);
        var minimumStock = InventoryValidation.ValidateStock(values.MinimumStock);
        ReconcileSuppliers(values.SupplierIds);

        Name = name;
        Status = values.Status;
        Notes = notes;
        CategoryId = values.CategoryId;
        LocationId = values.LocationId;
        CurrentStock = currentStock;
        MinimumStock = minimumStock;
        Visibility = values.Visibility;
        StampModification(actorId, now);
    }

    private void ReconcileSuppliers(IReadOnlyList<int> supplierIds)
    {
        ArgumentNullException.ThrowIfNull(supplierIds);
        var requested = new HashSet<int>(supplierIds);
        if (requested.Count == 0)
        {
            throw new InventoryValidationException(
                "An item requires at least one allowed supplier.",
                InventoryValidationReason.SupplierRequired);
        }

        if (requested.Any(id => id <= 0))
        {
            throw new InventoryValidationException("Supplier identifiers must be positive.");
        }

        suppliers.RemoveAll(association => !requested.Contains(association.SupplierId));
        foreach (var supplierId in requested)
        {
            if (suppliers.All(association => association.SupplierId != supplierId))
            {
                suppliers.Add(new InventoryItemSupplier { SupplierId = supplierId });
            }
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
            throw new InventoryValidationException("Technical timestamps must use UTC.");
        }
    }
}

/// <summary>
/// The many-to-many association between an item and a supplier allowed for its
/// future order lines. It carries no audit metadata of its own; the owning item's
/// modification metadata records changes to the association set.
/// </summary>
internal sealed class InventoryItemSupplier
{
    public int ItemId { get; set; }
    public int SupplierId { get; set; }
}

/// <summary>A single order line in a create or update operation, independent of identity.</summary>
internal sealed record InventoryOrderLineValues(int ItemId, decimal Quantity, decimal LineTotal);

/// <summary>The editable fields of an Inventory order, independent of audit metadata.</summary>
internal sealed record InventoryOrderValues(
    int SupplierId,
    InventoryOrderStatus Status,
    int CurrencyId,
    DateOnly? OrderDate,
    DateOnly? ExpectedReceiptDate,
    string? Notes,
    RecordVisibility Visibility,
    IReadOnlyList<InventoryOrderLineValues> Lines);

/// <summary>
/// A supplier-specific replenishment order. Every order belongs to exactly one
/// supplier and one currency, carries between 1 and 100 lines, and owns its lines
/// through full replacement. Stock changes only through the explicit receive
/// operation; manual status changes never move stock.
/// </summary>
internal sealed class InventoryOrder
{
    private readonly List<InventoryOrderLine> lines = [];

    private InventoryOrder()
    {
    }

    public int Id { get; private set; }
    public int SupplierId { get; private set; }
    public InventoryOrderStatus Status { get; private set; }
    public int CurrencyId { get; private set; }
    public DateOnly? OrderDate { get; private set; }
    public DateOnly? ExpectedReceiptDate { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }
    public IReadOnlyList<InventoryOrderLine> Lines => lines;

    public static InventoryOrder Create(InventoryOrderValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var order = new InventoryOrder
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        order.Apply(values, creatorId, now);
        return order;
    }

    public void Update(InventoryOrderValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now);
    }

    public void MarkReceived(UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (Status != InventoryOrderStatus.Active)
        {
            throw new InventoryValidationException("Only active orders can be received.");
        }

        Status = InventoryOrderStatus.Received;
        StampModification(actorId, now);
    }

    /// <summary>
    /// Re-points the order to <paramref name="supplierId"/> during a Configuration
    /// supplier migration. The supplier is required, so it is replaced rather than
    /// cleared; the order's lines and currency are unchanged.
    /// </summary>
    internal void ReplaceSupplier(int supplierId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (supplierId <= 0)
        {
            throw new InventoryValidationException("Catalog identifiers must be positive.");
        }

        SupplierId = supplierId;
        StampModification(actorId, now);
    }

    /// <summary>
    /// Converts every line total from the current currency to
    /// <paramref name="targetCurrencyId"/> using <paramref name="exchangeRate"/>
    /// (<c>1 source = exchangeRate target</c>), switches the currency, and stamps the
    /// modification. Quantities are unchanged. The owning Configuration command
    /// guarantees a positive rate with at most eight decimal places.
    /// </summary>
    internal void ConvertCurrency(int targetCurrencyId, decimal exchangeRate, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (targetCurrencyId <= 0)
        {
            throw new InventoryValidationException("Catalog identifiers must be positive.");
        }

        if (exchangeRate <= 0)
        {
            throw new InventoryValidationException("The exchange rate must be a positive value.");
        }

        foreach (var line in lines)
        {
            line.Convert(exchangeRate);
        }

        CurrencyId = targetCurrencyId;
        StampModification(actorId, now);
    }

    private void Apply(InventoryOrderValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        var notes = InventoryValidation.ValidateNotes(values.Notes);
        if (!Enum.IsDefined(values.Status) || !Enum.IsDefined(values.Visibility))
        {
            throw new InventoryValidationException("Status or visibility is invalid.");
        }

        if (values.SupplierId <= 0 || values.CurrencyId <= 0)
        {
            throw new InventoryValidationException("Catalog identifiers must be positive.");
        }

        ReplaceLines(values.Lines);

        SupplierId = values.SupplierId;
        Status = values.Status;
        CurrencyId = values.CurrencyId;
        OrderDate = values.OrderDate;
        ExpectedReceiptDate = values.ExpectedReceiptDate;
        Notes = notes;
        Visibility = values.Visibility;
        StampModification(actorId, now);
    }

    private void ReplaceLines(IReadOnlyList<InventoryOrderLineValues> lineValues)
    {
        ArgumentNullException.ThrowIfNull(lineValues);
        if (lineValues.Count < InventoryValidation.MinimumOrderLines
            || lineValues.Count > InventoryValidation.MaximumOrderLines)
        {
            throw new InventoryValidationException(
                $"An order must contain between {InventoryValidation.MinimumOrderLines} and "
                + $"{InventoryValidation.MaximumOrderLines} lines.");
        }

        lines.Clear();
        foreach (var line in lineValues)
        {
            lines.Add(InventoryOrderLine.Create(line));
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
            throw new InventoryValidationException("Technical timestamps must use UTC.");
        }
    }
}

/// <summary>
/// A single ordered line subordinate to exactly one order. It references the ordered
/// item, the ordered quantity, and the total price of the whole line in the parent
/// order's currency. Lines carry no independent audit metadata, currency, or
/// attachments.
/// </summary>
internal sealed class InventoryOrderLine
{
    private InventoryOrderLine()
    {
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    internal static InventoryOrderLine Create(InventoryOrderLineValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.ItemId <= 0)
        {
            throw new InventoryValidationException("An order line must reference an item.");
        }

        return new InventoryOrderLine
        {
            ItemId = values.ItemId,
            Quantity = InventoryValidation.ValidatePositiveQuantity(values.Quantity),
            LineTotal = InventoryValidation.ValidateLineTotal(values.LineTotal),
        };
    }

    /// <summary>
    /// Scales the line total by <paramref name="exchangeRate"/> and rounds to two
    /// decimal places during a parent-order currency conversion. The quantity is
    /// preserved and a zero total stays zero.
    /// </summary>
    internal void Convert(decimal exchangeRate)
    {
        LineTotal = decimal.Round(LineTotal * exchangeRate, 2, MidpointRounding.AwayFromZero);
    }
}

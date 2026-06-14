using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Capex.Domain;

internal sealed class CapexCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

internal sealed record CapexEntryValues(
    string Title,
    CapexMovementType MovementType,
    CapexEntryStatus Status,
    DateOnly DueDate,
    int CategoryId,
    int? SupplierId,
    int? CostCenterId,
    int CurrencyId,
    string? Notes,
    RecordVisibility Visibility);

internal sealed record CapexItemValues(string Description, decimal Quantity, decimal UnitAmount);

internal sealed class CapexEntry
{
    private readonly List<CapexItem> items = [];

    private CapexEntry()
    {
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public CapexMovementType MovementType { get; private set; }
    public CapexEntryStatus Status { get; private set; }
    public DateOnly DueDate { get; private set; }
    public int CategoryId { get; private set; }
    public int? SupplierId { get; private set; }
    public int? CostCenterId { get; private set; }
    public int CurrencyId { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }
    public IReadOnlyList<CapexItem> Items => items;

    public static CapexEntry Create(
        CapexEntryValues values,
        IReadOnlyList<CapexItemValues> itemValues,
        UserId creatorId,
        DateTimeOffset now)
    {
        EnsureUtc(now);
        var entry = new CapexEntry
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        entry.Apply(values, itemValues, creatorId, now, isCreation: true);
        return entry;
    }

    public void Update(
        CapexEntryValues values,
        IReadOnlyList<CapexItemValues> itemValues,
        UserId actorId,
        DateTimeOffset now)
    {
        Apply(values, itemValues, actorId, now, isCreation: false);
    }

    private void Apply(
        CapexEntryValues values,
        IReadOnlyList<CapexItemValues> itemValues,
        UserId actorId,
        DateTimeOffset now,
        bool isCreation)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(itemValues);
        EnsureUtc(now);

        var title = RequiredTrimmed(values.Title, 200, nameof(values.Title));
        var notes = OptionalTrimmed(values.Notes, 4000, nameof(values.Notes));
        if (!Enum.IsDefined(values.MovementType)
            || !Enum.IsDefined(values.Status)
            || !Enum.IsDefined(values.Visibility))
        {
            throw new CapexValidationException("Movement type, status, or visibility is invalid.");
        }
        if (values.CategoryId <= 0 || values.CurrencyId <= 0
            || values.SupplierId is <= 0 || values.CostCenterId is <= 0)
        {
            throw new CapexValidationException("Catalog identifiers must be positive.");
        }

        if (itemValues.Count is < 1 or > 100)
        {
            throw new CapexValidationException("An entry must contain between 1 and 100 items.");
        }

        if (!isCreation && values.Visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new CapexValidationException("Only the creator may change entry visibility.");
        }

        var replacement = itemValues.Select((item, position) =>
            CapexItem.Create(position, item)).ToList();

        Title = title;
        MovementType = values.MovementType;
        Status = values.Status;
        DueDate = values.DueDate;
        CategoryId = values.CategoryId;
        SupplierId = values.SupplierId;
        CostCenterId = values.CostCenterId;
        CurrencyId = values.CurrencyId;
        Notes = notes;
        Visibility = values.Visibility;
        items.Clear();
        items.AddRange(replacement);
        TotalAmount = CapexCalculations.CalculateTotal(items);
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    private static string RequiredTrimmed(string value, int maximumLength, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > maximumLength)
        {
            throw new CapexValidationException($"{field} is required and may contain at most {maximumLength} characters.");
        }

        return trimmed;
    }

    private static string? OptionalTrimmed(string? value, int maximumLength, string field)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
        {
            throw new CapexValidationException($"{field} may contain at most {maximumLength} characters.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new CapexValidationException("Technical timestamps must use UTC.");
        }
    }
}

internal sealed class CapexItem
{
    private CapexItem()
    {
    }

    public int Id { get; private set; }
    public int EntryId { get; private set; }
    public int Position { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitAmount { get; private set; }
    public decimal LineAmount { get; private set; }

    internal static CapexItem Create(int position, CapexItemValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var description = values.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description) || description.Length > 300)
        {
            throw new CapexValidationException("Item description is required and may contain at most 300 characters.");
        }

        if (values.Quantity <= 0 || values.UnitAmount < 0
            || decimal.Round(values.Quantity, 2) != values.Quantity
            || decimal.Round(values.UnitAmount, 2) != values.UnitAmount)
        {
            throw new CapexValidationException("Item quantities and amounts must be nonnegative values with at most two decimal places, and quantity must be positive.");
        }

        return new CapexItem
        {
            Position = position,
            Description = description,
            Quantity = values.Quantity,
            UnitAmount = values.UnitAmount,
            LineAmount = CapexCalculations.CalculateLineAmount(values.Quantity, values.UnitAmount),
        };
    }
}

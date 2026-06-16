namespace Segaris.Api.Modules.Inventory.Domain;

/// <summary>Domain limits and normalization rules shared by later Inventory Waves.</summary>
internal static class InventoryValidation
{
    public const int ItemNameMaximumLength = 200;
    public const int CategoryNameMaximumLength = 100;
    public const int LocationNameMaximumLength = 100;
    public const int NotesMaximumLength = 4000;
    public const int MinimumOrderLines = 1;
    public const int MaximumOrderLines = 100;

    public static string NormalizeItemName(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    public static string ValidateItemName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > ItemNameMaximumLength)
        {
            throw new InventoryValidationException(
                $"Name is required and may contain at most {ItemNameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string? ValidateNotes(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length > NotesMaximumLength)
        {
            throw new InventoryValidationException(
                $"Notes may contain at most {NotesMaximumLength} characters.");
        }

        return value.Length == 0 ? null : value;
    }

    /// <summary>
    /// Validates a stored stock level. Stock is nonnegative and has at most two
    /// decimal places.
    /// </summary>
    public static decimal ValidateStock(decimal value)
    {
        if (value < 0 || decimal.Round(value, 2) != value)
        {
            throw new InventoryValidationException(
                "Stock must be nonnegative and have at most two decimal places.");
        }

        return value;
    }

    /// <summary>
    /// Validates a strictly positive quantity such as an order-line quantity or a
    /// quick stock adjustment. The value has at most two decimal places.
    /// </summary>
    public static decimal ValidatePositiveQuantity(decimal value)
    {
        if (value <= 0 || decimal.Round(value, 2) != value)
        {
            throw new InventoryValidationException(
                "Quantities must be greater than zero and have at most two decimal places.");
        }

        return value;
    }

    /// <summary>
    /// Validates a line total price. The price is nonnegative and has at most two
    /// decimal places.
    /// </summary>
    public static decimal ValidateLineTotal(decimal value)
    {
        if (value < 0 || decimal.Round(value, 2) != value)
        {
            throw new InventoryValidationException(
                "Line totals must be nonnegative and have at most two decimal places.");
        }

        return value;
    }

    /// <summary>Applies a quick stock adjustment, rejecting a negative result.</summary>
    public static decimal ApplyStockAdjustment(
        decimal currentStock,
        InventoryStockAdjustmentDirection direction,
        decimal quantity)
    {
        ValidatePositiveQuantity(quantity);
        var result = direction == InventoryStockAdjustmentDirection.Increase
            ? currentStock + quantity
            : currentStock - quantity;

        if (result < 0)
        {
            throw new InventoryValidationException(
                "A stock reduction may not produce a negative result.",
                InventoryValidationReason.NegativeStock);
        }

        return ValidateStock(result);
    }
}

/// <summary>
/// Distinguishes the Inventory domain failures so the HTTP surface can map each one
/// to its frozen <see cref="InventoryErrorCodes"/> value.
/// </summary>
internal enum InventoryValidationReason
{
    /// <summary>A required string, length, enum, stock, or quantity rule failed.</summary>
    Validation,

    /// <summary>An item was saved without at least one allowed supplier.</summary>
    SupplierRequired,

    /// <summary>A stock reduction would produce a negative result.</summary>
    NegativeStock,
}

internal sealed class InventoryValidationException(
    string message,
    InventoryValidationReason reason = InventoryValidationReason.Validation) : Exception(message)
{
    public InventoryValidationReason Reason { get; } = reason;
}

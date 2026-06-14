namespace Segaris.Api.Modules.Capex.Domain;

/// <summary>
/// Distinguishes the Capex mutation failures so the HTTP surface can map each one
/// to its frozen <see cref="CapexErrorCodes"/> value.
/// </summary>
internal enum CapexValidationReason
{
    /// <summary>A required string, length, enum, item-count, or amount rule failed.</summary>
    Validation,

    /// <summary>A referenced category, supplier, cost center, or currency does not exist.</summary>
    CatalogReference,

    /// <summary>A non-creator attempted to change an entry's visibility.</summary>
    VisibilityForbidden,
}

internal sealed class CapexValidationException(
    string message,
    CapexValidationReason reason = CapexValidationReason.Validation) : Exception(message)
{
    public CapexValidationReason Reason { get; } = reason;
}

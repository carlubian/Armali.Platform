namespace Segaris.Api.Modules.Inventory.Domain;

/// <summary>
/// Normalization rules for the Inventory-owned category and location catalogs, kept
/// local to the module that owns them. It matches the shared-catalog rules: trim
/// exterior whitespace and fold to invariant upper-case for case-insensitive
/// uniqueness, without collapsing interior whitespace.
/// </summary>
internal static class InventoryCatalogNormalization
{
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

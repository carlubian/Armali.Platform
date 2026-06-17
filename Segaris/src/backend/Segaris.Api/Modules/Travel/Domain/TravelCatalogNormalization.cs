namespace Segaris.Api.Modules.Travel.Domain;

/// <summary>
/// Normalization rules for the Travel-owned trip-type and expense-category catalogs,
/// kept local to the module that owns them. It matches the shared-catalog rules: trim
/// exterior whitespace and fold to invariant upper-case for case-insensitive
/// uniqueness, without collapsing interior whitespace.
/// </summary>
internal static class TravelCatalogNormalization
{
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

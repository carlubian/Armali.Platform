namespace Segaris.Api.Modules.Capex.Domain;

/// <summary>
/// Normalization rules for the Capex-owned category catalog, kept local to the
/// module that owns the catalog. It matches the shared-catalog rules: trim exterior
/// whitespace and fold to invariant upper-case for case-insensitive uniqueness,
/// without collapsing interior whitespace.
/// </summary>
internal static class CapexCategoryNormalization
{
    /// <summary>The persisted maximum length of a category name.</summary>
    public const int NameMaximumLength = 100;

    public static string Normalize(string value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

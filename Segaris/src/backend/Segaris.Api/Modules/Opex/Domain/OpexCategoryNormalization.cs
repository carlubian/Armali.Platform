namespace Segaris.Api.Modules.Opex.Domain;

/// <summary>
/// Normalization rules for the Opex-owned category catalog, kept local to the
/// module that owns the catalog. It matches the shared-catalog rules: trim exterior
/// whitespace and fold to invariant upper-case for case-insensitive uniqueness,
/// without collapsing interior whitespace.
/// </summary>
internal static class OpexCategoryNormalization
{
    /// <summary>The persisted maximum length of a category name.</summary>
    public const int NameMaximumLength = OpexValidation.CategoryNameMaximumLength;

    public static string Normalize(string value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

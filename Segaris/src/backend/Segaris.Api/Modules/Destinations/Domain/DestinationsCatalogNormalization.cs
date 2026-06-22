namespace Segaris.Api.Modules.Destinations.Domain;

/// <summary>
/// Normalization rules for the Destinations-owned category catalogues, kept local to
/// the module that owns them. It matches the shared-catalog rules: trim exterior
/// whitespace and fold to invariant upper-case for case-insensitive uniqueness, without
/// collapsing interior whitespace.
/// </summary>
internal static class DestinationsCatalogNormalization
{
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

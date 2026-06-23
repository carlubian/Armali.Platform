namespace Segaris.Api.Modules.Health.Domain;

/// <summary>
/// Normalization rules for the Health-owned category catalogues, kept local to the
/// module that owns them. It matches the shared-catalogue rules: trim exterior
/// whitespace and fold to invariant upper-case for case-insensitive uniqueness,
/// without collapsing interior whitespace.
/// </summary>
internal static class HealthCatalogNormalization
{
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

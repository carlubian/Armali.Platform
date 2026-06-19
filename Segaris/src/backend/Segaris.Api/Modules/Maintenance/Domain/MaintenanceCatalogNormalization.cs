namespace Segaris.Api.Modules.Maintenance.Domain;

/// <summary>
/// Normalization rules for the Maintenance-owned <c>MaintenanceType</c> catalogue,
/// kept local to the module that owns it. It matches the shared-catalog rules: trim
/// exterior whitespace and fold to invariant upper-case for case-insensitive
/// uniqueness, without collapsing interior whitespace.
/// </summary>
internal static class MaintenanceCatalogNormalization
{
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

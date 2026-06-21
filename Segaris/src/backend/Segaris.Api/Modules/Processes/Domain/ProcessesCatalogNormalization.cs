namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>
/// Normalization rules for the Processes-owned <c>ProcessCategory</c> catalogue, kept
/// local to the module that owns it. It matches the shared-catalogue rules: trim exterior
/// whitespace and fold to invariant upper-case for case-insensitive uniqueness, without
/// collapsing interior whitespace.
/// </summary>
internal static class ProcessesCatalogNormalization
{
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

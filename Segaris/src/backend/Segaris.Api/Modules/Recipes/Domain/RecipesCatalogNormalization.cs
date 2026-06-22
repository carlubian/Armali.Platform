namespace Segaris.Api.Modules.Recipes.Domain;

/// <summary>
/// Normalization rules for the Recipes-owned category catalog, kept local to the
/// module that owns it. It matches the shared-catalog rules: trim exterior whitespace
/// and fold to invariant upper-case for case-insensitive uniqueness, without
/// collapsing interior whitespace.
/// </summary>
internal static class RecipesCatalogNormalization
{
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

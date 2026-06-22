using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Recipes.Contracts;

internal enum RecipesCatalogKind
{
    RecipeCategories,
}

/// <summary>
/// Frozen per-catalogue rules for the Recipes-owned catalogue surfaced through the
/// Configuration presentation boundary. A <see cref="RecipeCategory"/> is required
/// on every recipe, so it is never cleared and its referenced values may only be
/// replaced.
/// </summary>
internal sealed record RecipesCatalogDescriptor(
    RecipesCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing);

internal static class RecipesConfigurationContracts
{
    /// <summary>
    /// Recipes consumes no shared Configuration catalogue; its only catalogue is the
    /// module-owned <see cref="RecipeCategory"/>.
    /// </summary>
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<RecipesCatalogDescriptor> OwnedCatalogs =
    [
        new(RecipesCatalogKind.RecipeCategories, "categories", IsRequired: true, SupportsClearing: false),
    ];
}

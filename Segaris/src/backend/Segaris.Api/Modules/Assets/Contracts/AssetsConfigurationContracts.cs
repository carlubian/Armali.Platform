using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Assets.Contracts;

internal enum AssetCatalogKind
{
    AssetCategories,
    AssetLocations,
}

internal sealed record AssetCatalogDescriptor(
    AssetCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing);

/// <summary>
/// The Configuration contracts owned by Assets. Both the category and the location
/// catalogue are required on every asset, so each may only be replaced — never
/// cleared — when a referenced value is deleted. Assets publishes its own catalogue
/// entities and consumes no shared reference kinds.
/// </summary>
internal static class AssetsConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<AssetCatalogDescriptor> OwnedCatalogs =
    [
        new(AssetCatalogKind.AssetCategories, "categories", IsRequired: true, SupportsClearing: false),
        new(AssetCatalogKind.AssetLocations, "locations", IsRequired: true, SupportsClearing: false),
    ];
}

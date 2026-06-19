namespace Segaris.Api.Modules.Assets;

/// <summary>
/// Frozen initial values for the Assets-owned category and location catalogs,
/// inserted only once by the one-time initialization service. Like the shared
/// catalogs, rows use a database-assigned auto-increment <c>Id</c> and a
/// <c>SortOrder</c> following declaration order. Display names are canonical
/// <c>en-GB</c> values and are localizable in the presentation layer.
/// </summary>
internal static class AssetCatalog
{
    public static readonly IReadOnlyList<AssetCatalogSeed> Categories =
    [
        new("Furniture"),
        new("Appliances"),
        new("Electronics"),
        new("Vehicles"),
        new("Tools"),
        new("Other"),
    ];

    public static readonly IReadOnlyList<AssetCatalogSeed> Locations =
    [
        new("Living room"),
        new("Bedroom"),
        new("Kitchen"),
        new("Office"),
        new("Garage"),
        new("Storage room"),
        new("Outdoors"),
        new("Other"),
    ];
}

/// <summary>
/// A single frozen Assets catalog seed row identified only by its canonical display
/// <paramref name="Name"/>; ordering follows declaration order.
/// </summary>
internal sealed record AssetCatalogSeed(string Name);

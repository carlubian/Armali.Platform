namespace Segaris.Api.Modules.Inventory;

/// <summary>
/// Frozen initial values for the Inventory-owned category and location catalogs,
/// inserted only once by the one-time initialization service. Like the shared
/// catalogs, rows use a database-assigned auto-increment <c>Id</c> and a
/// <c>SortOrder</c> following declaration order. Display names are canonical
/// <c>en-GB</c> values and are localizable in the presentation layer.
/// </summary>
internal static class InventoryCatalog
{
    public static readonly IReadOnlyList<InventoryCatalogSeed> Categories =
    [
        new("Food"),
        new("Cleaning"),
        new("Hygiene"),
        new("Medicine"),
        new("Office"),
        new("Pets"),
        new("Other"),
    ];

    public static readonly IReadOnlyList<InventoryCatalogSeed> Locations =
    [
        new("Kitchen cabinet"),
        new("Pantry"),
        new("Bathroom"),
        new("Storage room"),
        new("Fridge"),
        new("Freezer"),
        new("Other"),
    ];
}

/// <summary>
/// A single frozen Inventory catalog seed row identified only by its canonical
/// display <paramref name="Name"/>; ordering follows declaration order.
/// </summary>
internal sealed record InventoryCatalogSeed(string Name);

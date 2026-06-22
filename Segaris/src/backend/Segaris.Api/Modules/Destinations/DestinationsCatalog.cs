namespace Segaris.Api.Modules.Destinations;

/// <summary>
/// Frozen initial values for the Destinations-owned category catalogues, inserted only
/// once by the one-time initialization service. Rows use a database-assigned
/// auto-increment <c>Id</c> and a <c>SortOrder</c> following declaration order. Display
/// names are canonical <c>en-GB</c> values and are localizable in the presentation
/// layer.
/// </summary>
internal static class DestinationsCatalog
{
    public static readonly IReadOnlyList<DestinationsCatalogSeed> DestinationCategories =
    [
        new("City"),
        new("Region"),
        new("Country"),
        new("Natural Area"),
        new("Other"),
    ];

    public static readonly IReadOnlyList<DestinationsCatalogSeed> PlaceCategories =
    [
        new("Hotel"),
        new("Restaurant"),
        new("Bar"),
        new("Café"),
        new("Museum"),
        new("Attraction"),
        new("Shop"),
        new("Other"),
    ];
}

/// <summary>
/// A single frozen Destinations catalogue seed row identified only by its canonical
/// display <paramref name="Name"/>; ordering follows declaration order.
/// </summary>
internal sealed record DestinationsCatalogSeed(string Name);

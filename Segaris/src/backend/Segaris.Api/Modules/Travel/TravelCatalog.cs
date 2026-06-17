namespace Segaris.Api.Modules.Travel;

/// <summary>
/// Frozen initial values for the Travel-owned trip-type and expense-category
/// catalogs, inserted only once by the one-time initialization service. Like the
/// shared catalogs, rows use a database-assigned auto-increment <c>Id</c> and a
/// <c>SortOrder</c> following declaration order. Display names are canonical
/// <c>en-GB</c> values and are localizable in the presentation layer.
/// </summary>
internal static class TravelCatalog
{
    public static readonly IReadOnlyList<TravelCatalogSeed> TripTypes =
    [
        new("Regional"),
        new("National"),
        new("European"),
        new("Non-Schengen"),
    ];

    public static readonly IReadOnlyList<TravelCatalogSeed> ExpenseCategories =
    [
        new("Flight"),
        new("Lodging"),
        new("Ground transport"),
        new("Meals"),
        new("Activities"),
        new("Other"),
    ];
}

/// <summary>
/// A single frozen Travel catalog seed row identified only by its canonical display
/// <paramref name="Name"/>; ordering follows declaration order.
/// </summary>
internal sealed record TravelCatalogSeed(string Name);

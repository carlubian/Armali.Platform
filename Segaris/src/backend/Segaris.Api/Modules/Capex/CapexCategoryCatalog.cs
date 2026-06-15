namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Frozen initial values for the Capex-owned category catalog
/// (<c>CapexCategory</c>), inserted only once by the one-time initialization
/// service. Like the shared catalogs, category rows use a database-assigned
/// auto-increment <c>Id</c> and a <c>SortOrder</c> following declaration order;
/// they no longer carry a stable code. Display names are canonical <c>en-GB</c>
/// values and are localizable in the presentation layer.
/// </summary>
internal static class CapexCategoryCatalog
{
    public static readonly IReadOnlyList<CapexCategorySeed> Categories =
    [
        new("Furniture"),
        new("Appliances"),
        new("Technology"),
        new("Home"),
        new("Food & Dining"),
        new("Leisure"),
        new("Health"),
        new("Transport"),
        new("Travel"),
        new("Education"),
        new("Gifts"),
        new("Taxes & Fees"),
        new("Salary & Income"),
        new("Other"),
    ];
}

/// <summary>
/// A single frozen Capex category seed row identified only by its canonical display
/// <paramref name="Name"/>; ordering follows declaration order.
/// </summary>
internal sealed record CapexCategorySeed(string Name);

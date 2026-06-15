namespace Segaris.Api.Modules.Opex;

/// <summary>
/// Frozen initial values for the Opex-owned category catalog
/// (<c>OpexCategory</c>), inserted only once by the one-time initialization
/// service. Like the shared catalogs, category rows use a database-assigned
/// auto-increment <c>Id</c> and a <c>SortOrder</c> following declaration order.
/// Display names are canonical <c>en-GB</c> values and are localizable in the
/// presentation layer.
/// </summary>
internal static class OpexCategoryCatalog
{
    public static readonly IReadOnlyList<OpexCategorySeed> Categories =
    [
        new("Housing"),
        new("Utilities"),
        new("Telecommunications"),
        new("Subscriptions"),
        new("Insurance"),
        new("Taxes & Fees"),
        new("Health"),
        new("Education"),
        new("Transport"),
        new("Employment"),
        new("Professional Services"),
        new("Financial Services"),
        new("Memberships"),
        new("Other"),
    ];
}

/// <summary>
/// A single frozen Opex category seed row identified only by its canonical display
/// <paramref name="Name"/>; ordering follows declaration order.
/// </summary>
internal sealed record OpexCategorySeed(string Name);

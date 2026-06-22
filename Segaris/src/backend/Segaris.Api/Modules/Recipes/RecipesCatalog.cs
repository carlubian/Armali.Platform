namespace Segaris.Api.Modules.Recipes;

/// <summary>
/// Frozen initial values for the Recipes-owned category catalog, inserted only once
/// by the one-time initialization service. Rows use a database-assigned auto-increment
/// <c>Id</c> and a <c>SortOrder</c> following declaration order. Display names are
/// canonical <c>en-GB</c> values and are localizable in the presentation layer.
/// </summary>
internal static class RecipesCatalog
{
    public static readonly IReadOnlyList<RecipesCatalogSeed> Categories =
    [
        new("Breakfast"),
        new("Starter"),
        new("Main"),
        new("Dessert"),
        new("Drink"),
        new("Sauce"),
        new("Side"),
        new("Other"),
    ];
}

/// <summary>
/// A single frozen Recipes catalog seed row identified only by its canonical display
/// <paramref name="Name"/>; ordering follows declaration order.
/// </summary>
internal sealed record RecipesCatalogSeed(string Name);

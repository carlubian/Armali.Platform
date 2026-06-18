namespace Segaris.Api.Modules.Clothes;

/// <summary>
/// Frozen initial values for the Clothes-owned category and colour catalogs, inserted
/// only once by the one-time initialization service. Like the shared catalogs, rows
/// use a database-assigned auto-increment <c>Id</c> and a <c>SortOrder</c> following
/// declaration order. Display names are canonical <c>en-GB</c> values and are
/// localizable in the presentation layer; colour values are canonical <c>#RRGGBB</c>
/// hex strings.
/// </summary>
internal static class ClothesCatalog
{
    public static readonly IReadOnlyList<ClothesCategorySeed> Categories =
    [
        new("Tops"),
        new("Bottoms"),
        new("Outerwear"),
        new("Dresses"),
        new("Footwear"),
        new("Underwear"),
        new("Sportswear"),
        new("Accessories"),
        new("Other"),
    ];

    public static readonly IReadOnlyList<ClothesColorSeed> Colors =
    [
        new("Black", "#000000"),
        new("White", "#FFFFFF"),
        new("Grey", "#808080"),
        new("Navy", "#1B2A4A"),
        new("Blue", "#2563EB"),
        new("Red", "#DC2626"),
        new("Green", "#16A34A"),
        new("Yellow", "#FACC15"),
        new("Orange", "#EA580C"),
        new("Brown", "#7C4A1E"),
        new("Beige", "#D8C3A5"),
        new("Pink", "#EC4899"),
        new("Purple", "#7C3AED"),
    ];
}

/// <summary>A frozen Clothes category seed row identified by its canonical display name.</summary>
internal sealed record ClothesCategorySeed(string Name);

/// <summary>A frozen Clothes colour seed row with its canonical display name and hex value.</summary>
internal sealed record ClothesColorSeed(string Name, string ColorValue);

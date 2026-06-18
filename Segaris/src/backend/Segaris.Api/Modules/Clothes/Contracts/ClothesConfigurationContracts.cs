using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Clothes.Contracts;

internal enum ClothesCatalogKind
{
    ClothingCategories,
    ClothingColors,
}

internal sealed record ClothesCatalogDescriptor(
    ClothesCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing,
    bool RequiresColorValue);

internal static class ClothesConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<ClothesCatalogDescriptor> OwnedCatalogs =
    [
        new(ClothesCatalogKind.ClothingCategories, "categories", IsRequired: true, SupportsClearing: false, RequiresColorValue: false),
        new(ClothesCatalogKind.ClothingColors, "colors", IsRequired: false, SupportsClearing: true, RequiresColorValue: true),
    ];
}

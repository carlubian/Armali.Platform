namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Frozen stable keys recorded by the internal one-time initialization table. Each
/// key distinguishes a never-initialized empty catalog from one an administrator
/// deliberately emptied: an unmarked empty catalog is seeded and marked, an
/// unmarked nonempty catalog is marked without mutation, and a marked catalog is
/// never seeded again. The upgrade migration marks all four existing catalogs so
/// current rows are preserved exactly.
///
/// The <c>capex.categories</c>, <c>opex.categories</c>, <c>inventory.categories</c>,
/// <c>inventory.locations</c>, <c>assets.categories</c>, <c>assets.locations</c>,
/// <c>maintenance.types</c>, <c>processes.categories</c>,
/// <c>firebird.person-categories</c>, <c>firebird.username-platforms</c>, and
/// <c>recipes.categories</c> keys name module-owned catalogs but live with the
/// other keys because the single initialization table is shared infrastructure.
/// </summary>
internal static class ConfigurationInitializationKeys
{
    public const string Suppliers = "configuration.suppliers";

    public const string CostCenters = "configuration.cost-centers";

    public const string Currencies = "configuration.currencies";

    public const string CapexCategories = "capex.categories";

    public const string OpexCategories = "opex.categories";

    public const string InventoryCategories = "inventory.categories";

    public const string InventoryLocations = "inventory.locations";

    public const string TravelTripTypes = "travel.trip-types";

    public const string TravelExpenseCategories = "travel.expense-categories";

    public const string ClothingCategories = "clothes.categories";

    public const string ClothingColors = "clothes.colors";

    public const string AssetCategories = "assets.categories";

    public const string AssetLocations = "assets.locations";

    public const string MaintenanceTypes = "maintenance.types";

    public const string ProcessCategories = "processes.categories";

    public const string FirebirdPersonCategories = "firebird.person-categories";

    public const string FirebirdUsernamePlatforms = "firebird.username-platforms";

    public const string RecipeCategories = "recipes.categories";

    public static IReadOnlyList<string> All { get; } =
    [
        Suppliers,
        CostCenters,
        Currencies,
        CapexCategories,
        OpexCategories,
        InventoryCategories,
        InventoryLocations,
        TravelTripTypes,
        TravelExpenseCategories,
        ClothingCategories,
        ClothingColors,
        AssetCategories,
        AssetLocations,
        MaintenanceTypes,
        ProcessCategories,
        FirebirdPersonCategories,
        FirebirdUsernamePlatforms,
        RecipeCategories,
    ];
}

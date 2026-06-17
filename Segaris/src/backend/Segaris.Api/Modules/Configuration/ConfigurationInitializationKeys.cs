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
/// and <c>inventory.locations</c> keys name module-owned catalogs but live with the
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

    public static IReadOnlyList<string> All { get; } =
    [
        Suppliers,
        CostCenters,
        Currencies,
        CapexCategories,
        OpexCategories,
        InventoryCategories,
        InventoryLocations,
    ];
}

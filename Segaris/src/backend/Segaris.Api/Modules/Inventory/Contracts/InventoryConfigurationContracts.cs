using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Inventory.Contracts;

/// <summary>
/// The Configuration contracts consumed by Inventory. Inventory keeps category
/// and location ownership and implements shared-catalog reference handlers for
/// suppliers and currencies without publishing its own entities.
/// </summary>
internal static class InventoryConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds =
    [
        ConfigurationCatalogKind.Suppliers,
        ConfigurationCatalogKind.Currencies,
    ];
}

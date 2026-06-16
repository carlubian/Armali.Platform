using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Opex.Contracts;

/// <summary>
/// The Configuration contracts consumed by Opex. Opex keeps category ownership
/// and implements shared-catalog reference handlers without publishing entities.
/// </summary>
internal static class OpexConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds =
    [
        ConfigurationCatalogKind.Suppliers,
        ConfigurationCatalogKind.CostCenters,
        ConfigurationCatalogKind.Currencies,
    ];
}

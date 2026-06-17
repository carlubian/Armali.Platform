using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Travel.Contracts;

/// <summary>
/// The Configuration contracts consumed by Travel. Travel owns trip types and
/// expense categories; shared supplier, currency, and cost-centre references are
/// consumed through Configuration's published catalog contracts.
/// </summary>
internal static class TravelConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds =
    [
        ConfigurationCatalogKind.Suppliers,
        ConfigurationCatalogKind.Currencies,
        ConfigurationCatalogKind.CostCenters,
    ];
}

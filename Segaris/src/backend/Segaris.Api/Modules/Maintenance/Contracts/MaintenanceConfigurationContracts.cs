using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Maintenance.Contracts;

internal enum MaintenanceCatalogKind
{
    MaintenanceTypes,
}

internal sealed record MaintenanceCatalogDescriptor(
    MaintenanceCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing);

/// <summary>
/// The Configuration contracts owned by Maintenance. A maintenance type is required
/// on every task, so the catalogue may only be replaced — never cleared — when a
/// referenced value is deleted, following the Assets category pattern. Maintenance
/// publishes its own catalogue entity and consumes no shared Configuration reference
/// kinds.
/// </summary>
internal static class MaintenanceConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<MaintenanceCatalogDescriptor> OwnedCatalogs =
    [
        new(MaintenanceCatalogKind.MaintenanceTypes, "types", IsRequired: true, SupportsClearing: false),
    ];
}

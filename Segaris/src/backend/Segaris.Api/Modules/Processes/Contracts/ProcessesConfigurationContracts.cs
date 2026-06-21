using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Processes.Contracts;

internal enum ProcessesCatalogKind
{
    ProcessCategories,
}

internal sealed record ProcessesCatalogDescriptor(
    ProcessesCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing);

/// <summary>
/// The Configuration contracts owned by Processes. A category is required on every
/// process, so the catalogue may only be replaced — never cleared — when a referenced
/// value is deleted, following the Maintenance and Assets category pattern. Processes
/// publishes its own catalogue entity and consumes no shared Configuration reference
/// kinds; Configuration never queries the Processes tables directly.
/// </summary>
internal static class ProcessesConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<ProcessesCatalogDescriptor> OwnedCatalogs =
    [
        new(ProcessesCatalogKind.ProcessCategories, "categories", IsRequired: true, SupportsClearing: false),
    ];
}

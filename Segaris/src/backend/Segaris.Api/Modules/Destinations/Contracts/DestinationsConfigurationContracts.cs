using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Destinations.Contracts;

internal enum DestinationsCatalogKind
{
    DestinationCategories,
    PlaceCategories,
}

/// <summary>
/// Frozen per-catalogue rules for Destinations-owned catalogues surfaced through
/// Configuration. Both references are required, so referenced values may only be
/// replaced and are never cleared.
/// </summary>
internal sealed record DestinationsCatalogDescriptor(
    DestinationsCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing);

internal static class DestinationsConfigurationContracts
{
    /// <summary>
    /// Destinations consumes no shared Configuration catalogue; its catalogues are
    /// module-owned and presented through Configuration.
    /// </summary>
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<DestinationsCatalogDescriptor> OwnedCatalogs =
    [
        new(DestinationsCatalogKind.DestinationCategories, "categories", IsRequired: true, SupportsClearing: false),
        new(DestinationsCatalogKind.PlaceCategories, "place-categories", IsRequired: true, SupportsClearing: false),
    ];
}

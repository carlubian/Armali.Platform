using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Wellness.Contracts;

/// <summary>The catalogues Wellness owns and surfaces through Configuration.</summary>
internal enum WellnessCatalogKind
{
    Tasks,
}

/// <summary>
/// Frozen per-catalogue rules for Wellness-owned catalogues surfaced through the
/// Configuration presentation boundary. The task catalogue is optional: it may be
/// empty, so its last remaining row can still be removed. Deletion is impact-free
/// because days hold task snapshots, so a task is never "referenced" and there is no
/// replacement flow (<see cref="SupportsClearing"/> is irrelevant and false).
/// </summary>
internal sealed record WellnessCatalogDescriptor(
    WellnessCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing);

internal static class WellnessConfigurationContracts
{
    /// <summary>
    /// Wellness consumes no shared Configuration catalogue: <c>WellnessTask</c> is
    /// fully module-owned and only presented through the Configuration boundary.
    /// </summary>
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<WellnessCatalogDescriptor> OwnedCatalogs =
    [
        new(WellnessCatalogKind.Tasks, "tasks", IsRequired: false, SupportsClearing: false),
    ];
}

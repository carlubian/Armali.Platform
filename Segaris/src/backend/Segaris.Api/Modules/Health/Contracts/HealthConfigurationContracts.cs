using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Health.Contracts;

internal enum HealthCatalogKind
{
    DiseaseCategories,
    MedicineCategories,
}

/// <summary>
/// Frozen per-catalogue rules for Health-owned catalogues surfaced through the
/// Configuration presentation boundary. Categories are required on every disease
/// and medicine, so referenced values may only be replaced.
/// </summary>
internal sealed record HealthCatalogDescriptor(
    HealthCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing);

internal static class HealthConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<HealthCatalogDescriptor> OwnedCatalogs =
    [
        new(HealthCatalogKind.DiseaseCategories, "disease-categories", IsRequired: true, SupportsClearing: false),
        new(HealthCatalogKind.MedicineCategories, "medicine-categories", IsRequired: true, SupportsClearing: false),
    ];
}

namespace Segaris.Api.Modules.Health;

/// <summary>
/// Frozen initial values for the Health-owned category catalogues, inserted only once
/// by the one-time initialization service. Rows use a database-assigned auto-increment
/// <c>Id</c> and a <c>SortOrder</c> following declaration order. Display names are
/// canonical <c>en-GB</c> values and are localizable in the presentation layer.
/// </summary>
internal static class HealthCatalog
{
    public static readonly IReadOnlyList<HealthCatalogSeed> DiseaseCategories =
    [
        new("Chronic"),
        new("Acute"),
        new("Infection"),
        new("Allergy"),
        new("Injury"),
        new("Other"),
    ];

    public static readonly IReadOnlyList<HealthCatalogSeed> MedicineCategories =
    [
        new("Analgesic"),
        new("Antibiotic"),
        new("Antihistamine"),
        new("Anti-inflammatory"),
        new("Supplement"),
        new("Topical"),
        new("Other"),
    ];
}

/// <summary>
/// A single frozen Health catalogue seed row identified only by its canonical display
/// <paramref name="Name"/>; ordering follows declaration order.
/// </summary>
internal sealed record HealthCatalogSeed(string Name);

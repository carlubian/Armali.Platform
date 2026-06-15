namespace Segaris.Api.Modules.Configuration.Contracts;

/// <summary>
/// The shared catalogs owned by the Configuration module. The kind is the stable
/// discriminator used to allow-list management routes, select reference-migration
/// handlers, and resolve the per-catalog replacement rules. Capex categories are a
/// Capex-owned catalog and deliberately do not appear here.
/// </summary>
internal enum ConfigurationCatalogKind
{
    Suppliers,
    CostCenters,
    Currencies,
}

/// <summary>
/// Frozen per-catalog rules that govern deletion and reference migration. These
/// describe the supported replacement modes (replace, clear, convert) and the
/// minimum-cardinality requirement, independent of any single consumer.
/// </summary>
/// <param name="Kind">The catalog this descriptor applies to.</param>
/// <param name="RouteSegment">
/// The lowercase URL segment used in management routes (the <c>{catalog}</c>
/// allow-list value).
/// </param>
/// <param name="IsRequired">
/// When <see langword="true"/> the catalog must keep at least one row, so its last
/// remaining value cannot be deleted even when unreferenced.
/// </param>
/// <param name="SupportsClearing">
/// When <see langword="true"/> a referenced value may be cleared to <c>null</c>
/// instead of replaced, because its consumer references are optional.
/// </param>
/// <param name="RequiresExchangeRateWhenReferenced">
/// When <see langword="true"/> deleting a referenced value requires a target value
/// and an explicit exchange rate (currency conversion).
/// </param>
internal sealed record ConfigurationCatalogDescriptor(
    ConfigurationCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing,
    bool RequiresExchangeRateWhenReferenced);

/// <summary>
/// Frozen lookup of the three shared Configuration catalogs and their replacement
/// rules. The collection order is the canonical order used by the administrative
/// experience.
/// </summary>
internal static class ConfigurationCatalogKinds
{
    public static readonly IReadOnlyList<ConfigurationCatalogDescriptor> All =
    [
        new(ConfigurationCatalogKind.Suppliers, "suppliers", IsRequired: false, SupportsClearing: true, RequiresExchangeRateWhenReferenced: false),
        new(ConfigurationCatalogKind.CostCenters, "cost-centers", IsRequired: false, SupportsClearing: true, RequiresExchangeRateWhenReferenced: false),
        new(ConfigurationCatalogKind.Currencies, "currencies", IsRequired: true, SupportsClearing: false, RequiresExchangeRateWhenReferenced: true),
    ];

    /// <summary>The allow-listed <c>{catalog}</c> route segments.</summary>
    public static IReadOnlyList<string> RouteSegments { get; } =
        All.Select(descriptor => descriptor.RouteSegment).ToArray();

    public static ConfigurationCatalogDescriptor ForKind(ConfigurationCatalogKind kind) =>
        All.Single(descriptor => descriptor.Kind == kind);

    public static bool TryResolveSegment(
        string? segment,
        out ConfigurationCatalogDescriptor descriptor)
    {
        descriptor = All.FirstOrDefault(candidate =>
            string.Equals(candidate.RouteSegment, segment, StringComparison.Ordinal))!;
        return descriptor is not null;
    }
}

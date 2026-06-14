namespace Segaris.Api.Modules.Configuration.Contracts;

/// <summary>
/// Narrow read and validation contract published by the Configuration module so
/// business modules (initially Capex) can list catalog values for forms and
/// filters and validate catalog references on mutation without reading
/// Configuration's EF Core entities.
///
/// The contract is intentionally internal to <c>Segaris.Api</c>: in this modular
/// monolith the cross-module boundary is enforced by namespace ownership and the
/// architecture tests, not by a separate assembly. Configuration entities stay
/// internal to the Configuration module.
/// </summary>
internal interface IConfigurationCatalog
{
    Task<IReadOnlyList<CatalogItem>> ListSuppliersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogItem>> ListCostCentersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogItem>> ListCurrenciesAsync(CancellationToken cancellationToken);

    Task<bool> SupplierExistsAsync(int supplierId, CancellationToken cancellationToken);

    Task<bool> CostCenterExistsAsync(int costCenterId, CancellationToken cancellationToken);

    Task<bool> CurrencyExistsAsync(int currencyId, CancellationToken cancellationToken);
}

/// <summary>
/// Bounded read model for a shared catalog value. <see cref="Code"/> is the
/// stable identity; <see cref="Name"/> is the localizable display value.
/// </summary>
internal sealed record CatalogItem(int Id, string Code, string Name);

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

    Task<IReadOnlyList<CurrencyItem>> ListCurrenciesAsync(CancellationToken cancellationToken);

    Task<bool> SupplierExistsAsync(int supplierId, CancellationToken cancellationToken);

    Task<bool> CostCenterExistsAsync(int costCenterId, CancellationToken cancellationToken);

    Task<bool> CurrencyExistsAsync(int currencyId, CancellationToken cancellationToken);
}

/// <summary>
/// Bounded read model for a non-currency shared catalog value. Identity is
/// <see cref="Id"/>; <see cref="Name"/> is the localizable display value and
/// <see cref="SortOrder"/> the deterministic catalog order.
/// </summary>
internal sealed record CatalogItem(int Id, string Name, int SortOrder);

/// <summary>
/// Bounded read model for a currency value. <see cref="Code"/> is the editable
/// three-letter display code; <see cref="Name"/> is localizable.
/// <see cref="ExchangeRateToEur"/> is the current rate to EUR, or
/// <see langword="null"/> when no rate has been configured yet.
/// </summary>
internal sealed record CurrencyItem(
    int Id,
    string Code,
    string Name,
    int SortOrder,
    decimal? ExchangeRateToEur);

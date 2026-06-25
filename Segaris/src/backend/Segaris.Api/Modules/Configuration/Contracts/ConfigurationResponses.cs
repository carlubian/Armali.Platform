namespace Segaris.Api.Modules.Configuration.Contracts;

/// <summary>
/// Frozen response contract for <c>GET /api/configuration/suppliers</c>.
/// </summary>
internal sealed record SupplierResponse(int Id, string Name, int SortOrder);

/// <summary>
/// Frozen response contract for <c>GET /api/configuration/cost-centers</c>.
/// </summary>
internal sealed record CostCenterResponse(int Id, string Name, int SortOrder);

/// <summary>
/// Frozen response contract for <c>GET /api/configuration/currencies</c>.
/// <see cref="Code"/> is the editable three-letter display code (for example
/// <c>EUR</c>). <see cref="ExchangeRateToEur"/> is the current rate to EUR
/// (<c>1 currency = ExchangeRateToEur EUR</c>); it is <see langword="null"/> only
/// for currencies migrated from before Analytics that never received a rate.
/// </summary>
internal sealed record CurrencyResponse(
    int Id,
    string Code,
    string Name,
    int SortOrder,
    decimal? ExchangeRateToEur);

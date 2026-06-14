namespace Segaris.Api.Modules.Configuration.Contracts;

/// <summary>
/// Frozen response contract for <c>GET /api/configuration/suppliers</c>.
/// </summary>
internal sealed record SupplierResponse(int Id, string Code, string Name);

/// <summary>
/// Frozen response contract for <c>GET /api/configuration/cost-centers</c>.
/// </summary>
internal sealed record CostCenterResponse(int Id, string Code, string Name);

/// <summary>
/// Frozen response contract for <c>GET /api/configuration/currencies</c>.
/// <see cref="Code"/> is the ISO 4217 code (for example <c>EUR</c>).
/// </summary>
internal sealed record CurrencyResponse(int Id, string Code, string Name);

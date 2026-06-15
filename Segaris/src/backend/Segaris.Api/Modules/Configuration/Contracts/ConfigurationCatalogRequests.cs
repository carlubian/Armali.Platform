namespace Segaris.Api.Modules.Configuration.Contracts;

/// <summary>
/// Frozen request contract for creating or updating a non-currency shared catalog
/// value (<c>POST /api/configuration/{catalog}</c> and
/// <c>PUT /api/configuration/{catalog}/{id}</c> for suppliers and cost centers).
/// The name is trimmed and unique case-insensitively within its catalog; the
/// server is authoritative for ordering and never trusts a client-supplied
/// position or identifier.
/// </summary>
internal sealed record CatalogItemRequest(string? Name);

/// <summary>
/// Frozen request contract for creating or updating a currency
/// (<c>POST /api/configuration/currencies</c> and
/// <c>PUT /api/configuration/currencies/{id}</c>). The code is a three-letter
/// upper-case display code, unique case-insensitively within the currency catalog.
/// </summary>
internal sealed record CurrencyItemRequest(string? Name, string? Code);

/// <summary>
/// Frozen request contract for <c>POST /api/configuration/{catalog}/{id}/move</c>.
/// <see cref="Direction"/> carries the wire vocabulary parsed by
/// <see cref="CatalogMoveDirections"/>.
/// </summary>
internal sealed record CatalogMoveRequest(string? Direction);

/// <summary>
/// Frozen request contract for
/// <c>POST /api/configuration/{catalog}/{id}/replace-and-delete</c>.
/// </summary>
/// <param name="ReplacementId">
/// The target value that inherits the source references. Required unless
/// <paramref name="ClearReferences"/> is <see langword="true"/>; must differ from
/// the source.
/// </param>
/// <param name="ClearReferences">
/// Clears optional references to <c>null</c> instead of replacing them. Allowed
/// only for catalogs whose consumer references are optional and mutually exclusive
/// with <paramref name="ReplacementId"/>.
/// </param>
/// <param name="ExchangeRate">
/// The positive exchange rate (at most eight decimal places) interpreted as
/// <c>1 source = ExchangeRate target</c>. Required only when deleting a referenced
/// currency.
/// </param>
internal sealed record CatalogReplacementRequest(
    int? ReplacementId,
    bool ClearReferences,
    decimal? ExchangeRate);

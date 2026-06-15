using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.IntegrationTests.Configuration;

/// <summary>
/// Builds the frozen administrator catalog request payloads so the management API
/// tests in Waves 2-5 share one setup. The endpoints themselves are mapped in
/// later waves; this builder only fixes the request shapes agreed in Wave 0.
/// </summary>
internal static class CatalogRequestBuilder
{
    public static CatalogItemRequest Item(string? name = "Test value") => new(name);

    public static CurrencyItemRequest Currency(string? name = "Test Currency", string? code = "TST") =>
        new(name, code);

    public static CatalogMoveRequest MoveUp() => new(CatalogMoveDirections.Up);

    public static CatalogMoveRequest MoveDown() => new(CatalogMoveDirections.Down);

    public static CatalogReplacementRequest ReplaceWith(int replacementId) =>
        new(replacementId, ClearReferences: false, ExchangeRate: null);

    public static CatalogReplacementRequest Clear() =>
        new(ReplacementId: null, ClearReferences: true, ExchangeRate: null);

    public static CatalogReplacementRequest ConvertTo(int replacementId, decimal exchangeRate) =>
        new(replacementId, ClearReferences: false, exchangeRate);
}

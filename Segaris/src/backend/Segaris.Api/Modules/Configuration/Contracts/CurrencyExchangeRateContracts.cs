namespace Segaris.Api.Modules.Configuration.Contracts;

internal sealed record CurrencyExchangeRateSnapshot(
    string CurrencyCode,
    decimal? ExchangeRateToEur);

internal interface ICurrencyExchangeRateProvider
{
    Task<IReadOnlyList<CurrencyExchangeRateSnapshot>> ListCurrentExchangeRatesAsync(
        CancellationToken cancellationToken);
}

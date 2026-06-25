using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Publishes the current currency exchange rates to EUR for Analytics read-time
/// normalization. It returns every currency's display code and current rate; a
/// <see langword="null"/> rate signals a currency without usable normalization so
/// Analytics can surface a configuration-incomplete state instead of summing it.
/// </summary>
internal sealed class CurrencyExchangeRateProvider(SegarisDbContext database)
    : ICurrencyExchangeRateProvider
{
    public async Task<IReadOnlyList<CurrencyExchangeRateSnapshot>> ListCurrentExchangeRatesAsync(
        CancellationToken cancellationToken) =>
        await database.Set<SegarisCurrency>()
            .AsNoTracking()
            .OrderBy(entity => entity.SortOrder)
            .ThenBy(entity => entity.Id)
            .Select(entity => new CurrencyExchangeRateSnapshot(entity.Code, entity.ExchangeRateToEur))
            .ToArrayAsync(cancellationToken);
}

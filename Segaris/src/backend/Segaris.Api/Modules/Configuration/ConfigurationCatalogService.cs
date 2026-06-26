using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Configuration;

internal sealed class ConfigurationCatalogService(SegarisDbContext database)
    : IConfigurationCatalog
{
    public async Task<IReadOnlyList<CatalogItem>> ListSuppliersAsync(
        CancellationToken cancellationToken) =>
        await database.Set<SegarisSupplier>()
            .AsNoTracking()
            .OrderBy(entity => entity.SortOrder)
            .ThenBy(entity => entity.Id)
            .Select(entity => new CatalogItem(entity.Id, entity.Name, entity.SortOrder))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogItem>> ListCostCentersAsync(
        CancellationToken cancellationToken) =>
        await database.Set<SegarisCostCenter>()
            .AsNoTracking()
            .OrderBy(entity => entity.SortOrder)
            .ThenBy(entity => entity.Id)
            .Select(entity => new CatalogItem(entity.Id, entity.Name, entity.SortOrder))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<CurrencyItem>> ListCurrenciesAsync(
        CancellationToken cancellationToken) =>
        await database.Set<SegarisCurrency>()
            .AsNoTracking()
            .OrderBy(entity => entity.SortOrder)
            .ThenBy(entity => entity.Id)
            .Select(entity => new CurrencyItem(entity.Id, entity.Code, entity.Name, entity.SortOrder, entity.ExchangeRateToEur))
            .ToArrayAsync(cancellationToken);

    public Task<bool> SupplierExistsAsync(int supplierId, CancellationToken cancellationToken) =>
        database.Set<SegarisSupplier>()
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == supplierId, cancellationToken);

    public Task<bool> CostCenterExistsAsync(int costCenterId, CancellationToken cancellationToken) =>
        database.Set<SegarisCostCenter>()
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == costCenterId, cancellationToken);

    public Task<bool> CurrencyExistsAsync(int currencyId, CancellationToken cancellationToken) =>
        database.Set<SegarisCurrency>()
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == currencyId, cancellationToken);
}

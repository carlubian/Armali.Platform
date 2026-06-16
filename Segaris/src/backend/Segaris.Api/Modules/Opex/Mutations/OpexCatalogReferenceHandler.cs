using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Opex.Mutations;

internal sealed class OpexCatalogReferenceHandler(SegarisDbContext database, ConfigurationCatalogKind kind)
    : ICatalogReferenceHandler
{
    public ConfigurationCatalogKind Kind => kind;

    public Task<bool> HasReferencesAsync(int catalogId, CancellationToken cancellationToken) => kind switch
    {
        ConfigurationCatalogKind.Suppliers => database.Set<OpexContract>().AnyAsync(contract => contract.SupplierId == catalogId, cancellationToken),
        ConfigurationCatalogKind.CostCenters => database.Set<OpexContract>().AnyAsync(contract => contract.CostCenterId == catalogId, cancellationToken),
        ConfigurationCatalogKind.Currencies => database.Set<OpexContract>().AnyAsync(contract => contract.CurrencyId == catalogId, cancellationToken),
        _ => Task.FromResult(false),
    };

    public async Task MigrateReferencesAsync(CatalogReferenceMigration migration, CancellationToken cancellationToken)
    {
        if (migration.Kind != kind)
        {
            throw new InvalidOperationException("The migration catalog does not match the registered handler.");
        }

        var contracts = kind switch
        {
            ConfigurationCatalogKind.Suppliers => await database.Set<OpexContract>()
                .Where(contract => contract.SupplierId == migration.SourceId)
                .ToListAsync(cancellationToken),
            ConfigurationCatalogKind.CostCenters => await database.Set<OpexContract>()
                .Where(contract => contract.CostCenterId == migration.SourceId)
                .ToListAsync(cancellationToken),
            ConfigurationCatalogKind.Currencies => await database.Set<OpexContract>()
                .Include(contract => contract.Occurrences)
                .Where(contract => contract.CurrencyId == migration.SourceId)
                .ToListAsync(cancellationToken),
            _ => throw new InvalidOperationException("The catalog is not supported by the Opex reference handler."),
        };

        foreach (var contract in contracts)
        {
            switch (kind)
            {
                case ConfigurationCatalogKind.Suppliers:
                    contract.ReplaceSupplier(migration.ClearReferences ? null : migration.ReplacementId, migration.Actor, migration.OccurredAt);
                    break;
                case ConfigurationCatalogKind.CostCenters:
                    contract.ReplaceCostCenter(migration.ClearReferences ? null : migration.ReplacementId, migration.Actor, migration.OccurredAt);
                    break;
                case ConfigurationCatalogKind.Currencies:
                    var target = migration.ReplacementId ?? throw new InvalidOperationException("Currency conversion requires a replacement currency.");
                    var rate = migration.ExchangeRate ?? throw new InvalidOperationException("Currency conversion requires an exchange rate.");
                    contract.ConvertCurrency(target, rate, migration.Actor, migration.OccurredAt);
                    break;
            }
        }
    }
}

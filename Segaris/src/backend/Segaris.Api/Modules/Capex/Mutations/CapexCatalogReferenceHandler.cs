using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Capex.Mutations;

internal sealed class CapexCatalogReferenceHandler(SegarisDbContext database, ConfigurationCatalogKind kind)
    : ICatalogReferenceHandler
{
    public ConfigurationCatalogKind Kind => kind;

    public Task<bool> HasReferencesAsync(int catalogId, CancellationToken cancellationToken) => kind switch
    {
        ConfigurationCatalogKind.Suppliers => database.Set<CapexEntry>().AnyAsync(entry => entry.SupplierId == catalogId, cancellationToken),
        ConfigurationCatalogKind.CostCenters => database.Set<CapexEntry>().AnyAsync(entry => entry.CostCenterId == catalogId, cancellationToken),
        ConfigurationCatalogKind.Currencies => database.Set<CapexEntry>().AnyAsync(entry => entry.CurrencyId == catalogId, cancellationToken),
        _ => Task.FromResult(false),
    };

    public async Task MigrateReferencesAsync(CatalogReferenceMigration migration, CancellationToken cancellationToken)
    {
        if (migration.Kind != kind)
        {
            throw new InvalidOperationException("The migration catalog does not match the registered handler.");
        }

        var entries = kind switch
        {
            ConfigurationCatalogKind.Suppliers => await database.Set<CapexEntry>()
                .Where(entry => entry.SupplierId == migration.SourceId)
                .ToListAsync(cancellationToken),
            ConfigurationCatalogKind.CostCenters => await database.Set<CapexEntry>()
                .Where(entry => entry.CostCenterId == migration.SourceId)
                .ToListAsync(cancellationToken),
            ConfigurationCatalogKind.Currencies => throw new NotSupportedException("Currency conversion is implemented in configuration Wave 5."),
            _ => throw new InvalidOperationException("The catalog is not supported by the Capex reference handler."),
        };

        foreach (var entry in entries)
        {
            if (kind == ConfigurationCatalogKind.Suppliers)
            {
                entry.ReplaceSupplier(migration.ClearReferences ? null : migration.ReplacementId, migration.Actor, migration.OccurredAt);
            }
            else
            {
                entry.ReplaceCostCenter(migration.ClearReferences ? null : migration.ReplacementId, migration.Actor, migration.OccurredAt);
            }
        }
    }
}

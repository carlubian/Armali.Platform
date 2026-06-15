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

    public Task MigrateReferencesAsync(CatalogReferenceMigration migration, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Reference migration is implemented in a later configuration wave.");
}

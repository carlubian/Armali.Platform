using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Travel.Mutations;

/// <summary>
/// Migrates shared Configuration catalog references held by Travel expenses. The
/// owning Configuration command invokes this handler inside its transaction; the
/// handler updates tracked entities only and never saves or commits.
/// </summary>
internal sealed class TravelCatalogReferenceHandler(SegarisDbContext database, ConfigurationCatalogKind kind)
    : ICatalogReferenceHandler
{
    public ConfigurationCatalogKind Kind => kind;

    public Task<bool> HasReferencesAsync(int catalogId, CancellationToken cancellationToken) => kind switch
    {
        ConfigurationCatalogKind.Suppliers => database.Set<TravelExpense>()
            .AnyAsync(expense => expense.SupplierId == catalogId, cancellationToken),
        ConfigurationCatalogKind.CostCenters => database.Set<TravelExpense>()
            .AnyAsync(expense => expense.CostCenterId == catalogId, cancellationToken),
        ConfigurationCatalogKind.Currencies => database.Set<TravelExpense>()
            .AnyAsync(expense => expense.CurrencyId == catalogId, cancellationToken),
        _ => Task.FromResult(false),
    };

    public async Task MigrateReferencesAsync(CatalogReferenceMigration migration, CancellationToken cancellationToken)
    {
        if (migration.Kind != kind)
        {
            throw new InvalidOperationException("The migration catalog does not match the registered handler.");
        }

        var expenses = kind switch
        {
            ConfigurationCatalogKind.Suppliers => await database.Set<TravelExpense>()
                .Where(expense => expense.SupplierId == migration.SourceId)
                .ToListAsync(cancellationToken),
            ConfigurationCatalogKind.CostCenters => await database.Set<TravelExpense>()
                .Where(expense => expense.CostCenterId == migration.SourceId)
                .ToListAsync(cancellationToken),
            ConfigurationCatalogKind.Currencies => await database.Set<TravelExpense>()
                .Where(expense => expense.CurrencyId == migration.SourceId)
                .ToListAsync(cancellationToken),
            _ => throw new InvalidOperationException("The catalog is not supported by the Travel reference handler."),
        };

        foreach (var expense in expenses)
        {
            switch (kind)
            {
                case ConfigurationCatalogKind.Suppliers:
                    expense.ReplaceSupplier(migration.ClearReferences ? null : migration.ReplacementId, migration.Actor, migration.OccurredAt);
                    break;
                case ConfigurationCatalogKind.CostCenters:
                    expense.ReplaceCostCenter(migration.ClearReferences ? null : migration.ReplacementId, migration.Actor, migration.OccurredAt);
                    break;
                case ConfigurationCatalogKind.Currencies:
                    var target = migration.ReplacementId
                        ?? throw new InvalidOperationException("Currency conversion requires a replacement currency.");
                    var rate = migration.ExchangeRate
                        ?? throw new InvalidOperationException("Currency conversion requires an exchange rate.");
                    expense.ConvertCurrency(target, rate, migration.Actor, migration.OccurredAt);
                    break;
            }
        }
    }
}

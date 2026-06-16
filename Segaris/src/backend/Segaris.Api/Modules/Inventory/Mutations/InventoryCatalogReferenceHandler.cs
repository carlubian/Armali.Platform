using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Inventory.Mutations;

/// <summary>
/// Migrates the shared Configuration catalogs that Inventory references (suppliers
/// and currencies) when an administrator deletes or replaces a value. It mirrors the
/// Capex and Opex handlers but reflects two Inventory-specific invariants: every
/// order requires a supplier and every item requires at least one allowed supplier,
/// so a referenced supplier may only be replaced, never cleared. The owning
/// Configuration command drives the handler inside its single transaction and never
/// lets the handler save or commit.
/// </summary>
internal sealed class InventoryCatalogReferenceHandler(SegarisDbContext database, ConfigurationCatalogKind kind)
    : ICatalogReferenceHandler
{
    public ConfigurationCatalogKind Kind => kind;

    public Task<bool> HasReferencesAsync(int catalogId, CancellationToken cancellationToken) => kind switch
    {
        ConfigurationCatalogKind.Suppliers => HasSupplierReferencesAsync(catalogId, cancellationToken),
        ConfigurationCatalogKind.Currencies => database.Set<InventoryOrder>().AnyAsync(order => order.CurrencyId == catalogId, cancellationToken),
        _ => Task.FromResult(false),
    };

    public async Task MigrateReferencesAsync(CatalogReferenceMigration migration, CancellationToken cancellationToken)
    {
        if (migration.Kind != kind)
        {
            throw new InvalidOperationException("The migration catalog does not match the registered handler.");
        }

        switch (kind)
        {
            case ConfigurationCatalogKind.Suppliers:
                await MigrateSupplierAsync(migration, cancellationToken);
                break;
            case ConfigurationCatalogKind.Currencies:
                await ConvertCurrencyAsync(migration, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("The catalog is not supported by the Inventory reference handler.");
        }
    }

    private async Task<bool> HasSupplierReferencesAsync(int supplierId, CancellationToken cancellationToken)
    {
        if (await database.Set<InventoryOrder>().AnyAsync(order => order.SupplierId == supplierId, cancellationToken))
        {
            return true;
        }

        return await database.Set<InventoryItemSupplier>().AnyAsync(association => association.SupplierId == supplierId, cancellationToken);
    }

    private async Task MigrateSupplierAsync(CatalogReferenceMigration migration, CancellationToken cancellationToken)
    {
        if (migration.ClearReferences)
        {
            // Orders always require a supplier and items always require at least one
            // allowed supplier, so a supplier Inventory still references can only be
            // replaced. Reject the clearing migration; the owning command rolls the
            // whole transaction back and reports a stable replacement-required conflict.
            if (await HasSupplierReferencesAsync(migration.SourceId, cancellationToken))
            {
                throw new CatalogReplacementRequiredException(
                    "Inventory orders and items require a supplier, so the supplier must be replaced rather than cleared.");
            }

            return;
        }

        var replacementId = migration.ReplacementId
            ?? throw new InvalidOperationException("Supplier replacement requires a replacement supplier.");

        var orders = await database.Set<InventoryOrder>()
            .Where(order => order.SupplierId == migration.SourceId)
            .ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            order.ReplaceSupplier(replacementId, migration.Actor, migration.OccurredAt);
        }

        var items = await database.Set<InventoryItem>()
            .Include(item => item.Suppliers)
            .Where(item => item.Suppliers.Any(association => association.SupplierId == migration.SourceId))
            .ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            item.ReplaceSupplier(migration.SourceId, replacementId, migration.Actor, migration.OccurredAt);
        }
    }

    private async Task ConvertCurrencyAsync(CatalogReferenceMigration migration, CancellationToken cancellationToken)
    {
        var target = migration.ReplacementId
            ?? throw new InvalidOperationException("Currency conversion requires a replacement currency.");
        var rate = migration.ExchangeRate
            ?? throw new InvalidOperationException("Currency conversion requires an exchange rate.");

        var orders = await database.Set<InventoryOrder>()
            .Include(order => order.Lines)
            .Where(order => order.CurrencyId == migration.SourceId)
            .ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            order.ConvertCurrency(target, rate, migration.Actor, migration.OccurredAt);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Inventory;

/// <summary>
/// Seeds Inventory items directly through the database for the Wave 2 read, attention,
/// and stock-adjustment tests, before the Wave 3 item mutation endpoints exist.
/// Catalog references are resolved by their display name; suppliers come from the
/// Configuration-seeded shared catalog.
/// </summary>
internal static class InventoryTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> SupplierIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisSupplier>().Where(supplier => supplier.Name == name).Select(supplier => supplier.Id).SingleAsync();
    }

    public static async Task<int> CurrencyIdAsync(
        IServiceProvider services,
        string code = ConfigurationCatalog.CurrencyCodes.Default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisCurrency>().Where(currency => currency.Code == code).Select(currency => currency.Id).SingleAsync();
    }

    public static async Task<int> CategoryIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<InventoryCategory>().Where(category => category.Name == name).Select(category => category.Id).SingleAsync();
    }

    public static async Task<int> LocationIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<InventoryLocation>().Where(location => location.Name == name).Select(location => location.Id).SingleAsync();
    }

    public static async Task<decimal> CurrentStockAsync(IServiceProvider services, int itemId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<InventoryItem>().Where(item => item.Id == itemId).Select(item => item.CurrentStock).SingleAsync();
    }

    public static async Task<bool> ItemExistsAsync(IServiceProvider services, int itemId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<InventoryItem>().AnyAsync(item => item.Id == itemId);
    }

    public static async Task<bool> OrderExistsAsync(IServiceProvider services, int orderId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<InventoryOrder>().AnyAsync(order => order.Id == orderId);
    }

    public static async Task<int> SeedItemAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Item",
        InventoryItemStatus status = InventoryItemStatus.Active,
        string? notes = null,
        string categoryName = "Other",
        string locationName = "Other",
        decimal currentStock = 0m,
        decimal minimumStock = 0m,
        IReadOnlyList<string>? supplierNames = null,
        RecordVisibility visibility = RecordVisibility.Public)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<InventoryCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();
        var locationId = await database.Set<InventoryLocation>()
            .Where(location => location.Name == locationName)
            .Select(location => location.Id)
            .SingleAsync();

        var names = supplierNames ?? ["Amazon"];
        var supplierIds = await database.Set<SegarisSupplier>()
            .Where(supplier => names.Contains(supplier.Name))
            .Select(supplier => supplier.Id)
            .ToListAsync();

        var values = new InventoryItemValues(
            name,
            status,
            notes,
            categoryId,
            locationId,
            currentStock,
            minimumStock,
            supplierIds,
            visibility);

        var item = InventoryItem.Create(values, new UserId(creatorId), SeedNow);
        database.Add(item);
        await database.SaveChangesAsync();
        return item.Id;
    }

    public static async Task<int> SeedOrderAsync(
        IServiceProvider services,
        int creatorId,
        int itemId,
        string supplierName = "Amazon",
        InventoryOrderStatus status = InventoryOrderStatus.Active,
        RecordVisibility visibility = RecordVisibility.Public,
        string? notes = null,
        DateOnly? orderDate = null,
        DateOnly? expectedReceiptDate = null,
        decimal quantity = 1m,
        decimal lineTotal = 10m)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var supplierId = await database.Set<SegarisSupplier>()
            .Where(supplier => supplier.Name == supplierName)
            .Select(supplier => supplier.Id)
            .SingleAsync();
        var currencyId = await database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default)
            .Select(currency => currency.Id)
            .SingleAsync();

        var values = new InventoryOrderValues(
            supplierId,
            status,
            currencyId,
            orderDate ?? DateOnly.FromDateTime(SeedNow.UtcDateTime.Date),
            expectedReceiptDate ?? DateOnly.FromDateTime(SeedNow.UtcDateTime.Date.AddDays(7)),
            notes,
            visibility,
            [new InventoryOrderLineValues(itemId, quantity, lineTotal)]);

        var order = InventoryOrder.Create(values, new UserId(creatorId), SeedNow);
        database.Add(order);
        await database.SaveChangesAsync();
        return order.Id;
    }
}

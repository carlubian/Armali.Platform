using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Capex;

/// <summary>
/// Seeds Capex entries directly through the database for the Wave 3 read-API
/// tests, before the Wave 4 mutation endpoints exist. Catalog references are
/// resolved by their stable codes so the seeds stay readable.
/// </summary>
internal static class CapexTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> CategoryIdAsync(IServiceProvider services, string code)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<CapexCategory>().Where(category => category.Code == code).Select(category => category.Id).SingleAsync();
    }

    public static async Task<int> SupplierIdAsync(IServiceProvider services, string code)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisSupplier>().Where(supplier => supplier.Code == code).Select(supplier => supplier.Id).SingleAsync();
    }

    public static async Task<int> CostCenterIdAsync(IServiceProvider services, string code)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisCostCenter>().Where(costCenter => costCenter.Code == code).Select(costCenter => costCenter.Id).SingleAsync();
    }

    public static async Task<int> CurrencyIdAsync(IServiceProvider services, string code)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisCurrency>().Where(currency => currency.Code == code).Select(currency => currency.Id).SingleAsync();
    }

    public static async Task<int> SeedEntryAsync(
        IServiceProvider services,
        int creatorId,
        string title = "Entry",
        CapexMovementType movementType = CapexMovementType.Expense,
        CapexEntryStatus status = CapexEntryStatus.Planning,
        DateOnly? dueDate = null,
        string categoryCode = CapexCategoryCatalog.Codes.Other,
        string? supplierCode = null,
        string? costCenterCode = null,
        string currencyCode = ConfigurationCatalog.CurrencyCodes.Default,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public,
        IReadOnlyList<CapexItemValues>? items = null)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<CapexCategory>()
            .Where(category => category.Code == categoryCode)
            .Select(category => category.Id)
            .SingleAsync();
        var currencyId = await database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == currencyCode)
            .Select(currency => currency.Id)
            .SingleAsync();
        var supplierId = supplierCode is null
            ? (int?)null
            : await database.Set<SegarisSupplier>()
                .Where(supplier => supplier.Code == supplierCode)
                .Select(supplier => (int?)supplier.Id)
                .SingleAsync();
        var costCenterId = costCenterCode is null
            ? (int?)null
            : await database.Set<SegarisCostCenter>()
                .Where(costCenter => costCenter.Code == costCenterCode)
                .Select(costCenter => (int?)costCenter.Id)
                .SingleAsync();

        var values = new CapexEntryValues(
            title,
            movementType,
            status,
            dueDate ?? new DateOnly(2026, 6, 14),
            categoryId,
            supplierId,
            costCenterId,
            currencyId,
            notes,
            visibility);

        var entry = CapexEntry.Create(
            values,
            items ?? [new CapexItemValues(title, 1m, 0m)],
            new UserId(creatorId),
            SeedNow);

        database.Add(entry);
        await database.SaveChangesAsync();
        return entry.Id;
    }
}

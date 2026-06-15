using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Opex;

/// <summary>
/// Seeds Opex contracts and occurrences directly through the database for the
/// Wave 2 read-API tests, before the Wave 3 and Wave 4 mutation endpoints exist.
/// Non-currency catalog references are resolved by their display name; currencies
/// are resolved by their editable code.
/// </summary>
internal static class OpexTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> CategoryIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<OpexCategory>().Where(category => category.Name == name).Select(category => category.Id).SingleAsync();
    }

    public static async Task<int> SupplierIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisSupplier>().Where(supplier => supplier.Name == name).Select(supplier => supplier.Id).SingleAsync();
    }

    public static async Task<int> CostCenterIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisCostCenter>().Where(costCenter => costCenter.Name == name).Select(costCenter => costCenter.Id).SingleAsync();
    }

    public static async Task<int> CurrencyIdAsync(IServiceProvider services, string code)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisCurrency>().Where(currency => currency.Code == code).Select(currency => currency.Id).SingleAsync();
    }

    public static async Task<int> OccurrenceCountAsync(IServiceProvider services, int contractId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<OpexOccurrence>().CountAsync(occurrence => occurrence.ContractId == contractId);
    }

    public static async Task<int> SeedContractAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Contract",
        OpexMovementType movementType = OpexMovementType.Expense,
        OpexContractStatus status = OpexContractStatus.Active,
        OpexExpectedFrequency frequency = OpexExpectedFrequency.Monthly,
        decimal? estimatedAnnualAmount = null,
        string categoryName = "Other",
        string? supplierName = null,
        string? costCenterName = null,
        string currencyCode = ConfigurationCatalog.CurrencyCodes.Default,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public,
        IReadOnlyList<(DateOnly EffectiveDate, decimal Amount)>? occurrences = null)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<OpexCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();
        var currencyId = await database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == currencyCode)
            .Select(currency => currency.Id)
            .SingleAsync();
        var supplierId = supplierName is null
            ? (int?)null
            : await database.Set<SegarisSupplier>()
                .Where(supplier => supplier.Name == supplierName)
                .Select(supplier => (int?)supplier.Id)
                .SingleAsync();
        var costCenterId = costCenterName is null
            ? (int?)null
            : await database.Set<SegarisCostCenter>()
                .Where(costCenter => costCenter.Name == costCenterName)
                .Select(costCenter => (int?)costCenter.Id)
                .SingleAsync();

        var values = new OpexContractValues(
            name,
            movementType,
            status,
            StartDate: null,
            ClosedDate: null,
            estimatedAnnualAmount,
            frequency,
            categoryId,
            supplierId,
            costCenterId,
            currencyId,
            notes,
            visibility);

        var contract = OpexContract.Create(values, new UserId(creatorId), SeedNow);
        database.Add(contract);
        await database.SaveChangesAsync();

        if (occurrences is { Count: > 0 })
        {
            foreach (var (effectiveDate, amount) in occurrences)
            {
                var occurrence = OpexOccurrence.Create(
                    contract.Id,
                    new OpexOccurrenceValues(effectiveDate, amount, Description: null, Notes: null),
                    new UserId(creatorId),
                    SeedNow);
                database.Add(occurrence);
            }

            await database.SaveChangesAsync();
        }

        return contract.Id;
    }
}

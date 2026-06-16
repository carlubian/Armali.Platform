using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Opex;

public sealed class OpexConfigurationMigrationTests
{
    [Fact]
    public async Task Supplier_replacement_migrates_public_and_private_contracts_and_audits_the_admin()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-supplier-owner", "MemberPass123!");
        var publicContractId = await OpexTestData.SeedContractAsync(
            server.Services, adminId, name: "Public Netflix", supplierName: "Amazon");
        var privateContractId = await OpexTestData.SeedContractAsync(
            server.Services, memberId, name: "Private Netflix", supplierName: "Amazon", visibility: RecordVisibility.Private);
        var sourceId = await OpexTestData.SupplierIdAsync(server.Services, "Amazon");
        var replacementId = await OpexTestData.SupplierIdAsync(server.Services, "IKEA");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/suppliers/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var contracts = await database.Set<OpexContract>()
            .Where(contract => contract.Id == publicContractId || contract.Id == privateContractId)
            .OrderBy(contract => contract.Id)
            .ToArrayAsync();
        Assert.All(contracts, contract =>
        {
            Assert.Equal(replacementId, contract.SupplierId);
            Assert.Equal(adminId, contract.UpdatedBy);
            Assert.Equal(TimeSpan.Zero, contract.UpdatedAt.Offset);
        });
        Assert.False(await database.Set<SegarisSupplier>().AnyAsync(supplier => supplier.Id == sourceId));
    }

    [Fact]
    public async Task Cost_center_references_can_be_cleared_on_public_and_private_contracts()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-cost-center-owner", "MemberPass123!");
        var publicContractId = await OpexTestData.SeedContractAsync(
            server.Services, adminId, name: "Public subscription", costCenterName: "Household");
        var privateContractId = await OpexTestData.SeedContractAsync(
            server.Services, memberId, name: "Private subscription", costCenterName: "Household", visibility: RecordVisibility.Private);
        var sourceId = await OpexTestData.CostCenterIdAsync(server.Services, "Household");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/cost-centers/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var contracts = await database.Set<OpexContract>()
            .Where(contract => contract.Id == publicContractId || contract.Id == privateContractId)
            .ToArrayAsync();
        Assert.All(contracts, contract =>
        {
            Assert.Null(contract.CostCenterId);
            Assert.Equal(adminId, contract.UpdatedBy);
        });
        Assert.False(await database.Set<SegarisCostCenter>().AnyAsync(cc => cc.Id == sourceId));
    }

    [Fact]
    public async Task Currency_conversion_recalculates_estimate_and_occurrence_amounts_for_public_and_private_contracts()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-currency-owner", "MemberPass123!");
        var publicContractId = await OpexTestData.SeedContractAsync(
            server.Services,
            adminId,
            name: "Public streaming",
            currencyCode: "EUR",
            estimatedAnnualAmount: 100.00m,
            occurrences: [(new DateOnly(2026, 1, 15), 10.00m), (new DateOnly(2026, 3, 20), 5.55m)]);
        var privateContractId = await OpexTestData.SeedContractAsync(
            server.Services,
            memberId,
            name: "Private streaming",
            currencyCode: "EUR",
            estimatedAnnualAmount: null,
            visibility: RecordVisibility.Private,
            occurrences: [(new DateOnly(2026, 2, 1), 0.00m)]);
        var sourceId = await OpexTestData.CurrencyIdAsync(server.Services, "EUR");
        var targetId = await OpexTestData.CurrencyIdAsync(server.Services, "USD");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/currencies/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(targetId, ClearReferences: false, ExchangeRate: 1.20m),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var publicContract = await database.Set<OpexContract>()
            .Include(contract => contract.Occurrences)
            .SingleAsync(contract => contract.Id == publicContractId);
        Assert.Equal(targetId, publicContract.CurrencyId);
        Assert.Equal(adminId, publicContract.UpdatedBy);
        Assert.Equal(TimeSpan.Zero, publicContract.UpdatedAt.Offset);
        // 100.00 * 1.20 = 120.00
        Assert.Equal(120.00m, publicContract.EstimatedAnnualAmount);
        var publicOccurrences = publicContract.Occurrences.OrderBy(o => o.EffectiveDate).ToArray();
        // 10.00 * 1.20 = 12.00; 5.55 * 1.20 = 6.66
        Assert.Equal(12.00m, publicOccurrences[0].ActualAmount);
        Assert.Equal(6.66m, publicOccurrences[1].ActualAmount);
        Assert.All(publicOccurrences, o => Assert.Equal(adminId, o.UpdatedBy));

        var privateContract = await database.Set<OpexContract>()
            .Include(contract => contract.Occurrences)
            .SingleAsync(contract => contract.Id == privateContractId);
        Assert.Equal(targetId, privateContract.CurrencyId);
        Assert.Equal(adminId, privateContract.UpdatedBy);
        Assert.Null(privateContract.EstimatedAnnualAmount);
        // 0.00 * 1.20 = 0.00
        var privateOccurrence = Assert.Single(privateContract.Occurrences);
        Assert.Equal(0.00m, privateOccurrence.ActualAmount);
        Assert.Equal(adminId, privateOccurrence.UpdatedBy);

        Assert.False(await database.Set<SegarisCurrency>().AnyAsync(currency => currency.Id == sourceId));
    }

    [Fact]
    public async Task Referenced_currency_deletion_requires_an_exchange_rate_and_leaves_opex_unchanged()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var contractId = await OpexTestData.SeedContractAsync(
            server.Services, adminId, name: "Uses euro", currencyCode: "EUR");
        var sourceId = await OpexTestData.CurrencyIdAsync(server.Services, "EUR");
        var targetId = await OpexTestData.CurrencyIdAsync(server.Services, "USD");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/currencies/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(targetId, ClearReferences: false, ExchangeRate: null),
            csrf);

        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("configuration.catalog.exchange_rate_required", problem!.Code);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var contract = await database.Set<OpexContract>().SingleAsync(contract => contract.Id == contractId);
        Assert.Equal(sourceId, contract.CurrencyId);
        Assert.True(await database.Set<SegarisCurrency>().AnyAsync(currency => currency.Id == sourceId));
    }

    [Fact]
    public async Task Opex_supplier_impact_reports_referenced_state_without_disclosing_private_contracts()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync("private-supplier-impact", "MemberPass123!");
        await OpexTestData.SeedContractAsync(
            server.Services, memberId, name: "Private contract", supplierName: "Amazon", visibility: RecordVisibility.Private);
        var sourceId = await OpexTestData.SupplierIdAsync(server.Services, "Amazon");
        using var client = await server.CreateAuthenticatedClientAsync();

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/configuration/suppliers/{sourceId}/deletion-impact",
            CancellationToken.None);

        Assert.True(impact!.IsReferenced);
        Assert.False(impact.CanDeleteDirectly);
        Assert.True(impact.CanClearReferences);
    }

    private sealed record ProblemPayload(string? Code);
}

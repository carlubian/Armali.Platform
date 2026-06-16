using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Opex;

public sealed class OpexContractDetailTests
{
    private static readonly TimeZoneInfo Household = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    private static int CurrentYear =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Household).Year;

    private static DateOnly YearStart => new(CurrentYear, 1, 1);

    private static DateOnly YearEnd => new(CurrentYear, 12, 31);

    [Fact]
    public async Task Detail_requires_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/opex/contracts/1", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Detail_returns_the_full_projection_with_resolved_names()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var contractId = await OpexTestData.SeedContractAsync(
            server.Services,
            founderId,
            name: "Internet line",
            movementType: OpexMovementType.Expense,
            status: OpexContractStatus.Active,
            frequency: OpexExpectedFrequency.Monthly,
            estimatedAnnualAmount: 480m,
            categoryName: "Telecommunications",
            supplierName: "Amazon",
            costCenterName: "Household",
            currencyCode: ConfigurationCatalog.CurrencyCodes.Euro,
            notes: "Fibre contract");

        var contract = await client.GetFromJsonAsync<OpexContractResponse>(
            $"/api/opex/contracts/{contractId}", CancellationToken.None);

        Assert.NotNull(contract);
        Assert.Equal("Internet line", contract.Name);
        Assert.Equal("Expense", contract.MovementType);
        Assert.Equal("Active", contract.Status);
        Assert.Equal("Monthly", contract.ExpectedFrequency);
        Assert.Equal(480m, contract.EstimatedAnnualAmount);
        Assert.Equal("Telecommunications", contract.CategoryName);
        Assert.Equal("Amazon", contract.SupplierName);
        Assert.Equal("Household", contract.CostCenterName);
        Assert.Equal(ConfigurationCatalog.CurrencyCodes.Euro, contract.CurrencyCode);
        Assert.Equal("Fibre contract", contract.Notes);
        Assert.Equal("Public", contract.Visibility);
        Assert.Equal(CapexTestServer.AdminUserName, contract.CreatedByName);
        Assert.Empty(contract.Attachments);
    }

    [Fact]
    public async Task Detail_returns_not_found_for_a_missing_contract()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync("/api/opex/contracts/999999", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.contract.not_found", problem!.Code);
    }

    [Fact]
    public async Task Another_users_private_contract_is_indistinguishable_from_missing()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        var privateId = await OpexTestData.SeedContractAsync(
            server.Services, memberId, name: "Member private", visibility: RecordVisibility.Private);

        using var response = await admin.GetAsync($"/api/opex/contracts/{privateId}", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        // The private record is hidden with the same not-found response a missing one returns.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.contract.not_found", problem!.Code);
    }

    [Fact]
    public async Task Realized_amount_is_zero_for_a_contract_without_occurrences()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Empty");

        var summary = Assert.Single((await GetPageAsync(client, "/api/opex/contracts")).Items);

        Assert.Equal(0m, summary.RealizedCurrentYearAmount);
    }

    [Fact]
    public async Task Realized_amount_sums_only_current_year_occurrences_within_the_madrid_boundary()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(
            server.Services,
            founderId,
            name: "With occurrences",
            occurrences:
            [
                (YearStart, 100.50m),                 // first day of the current year (inclusive)
                (YearEnd, 49.50m),                    // last day of the current year (inclusive)
                (YearStart.AddDays(-1), 1000m),       // previous year, excluded
                (YearEnd.AddDays(1), 2000m),          // next year, excluded
            ]);

        var summary = Assert.Single((await GetPageAsync(client, "/api/opex/contracts")).Items);

        Assert.Equal(150m, summary.RealizedCurrentYearAmount);
    }

    [Fact]
    public async Task Realized_amount_includes_zero_amount_occurrences_and_ignores_contract_status()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(
            server.Services,
            founderId,
            name: "Closed with movement",
            status: OpexContractStatus.Closed,
            occurrences: [(YearStart, 0m), (YearEnd, 25m)]);

        var summary = Assert.Single((await GetPageAsync(client, "/api/opex/contracts")).Items);

        // Status does not gate aggregation; a zero occurrence contributes nothing but is valid.
        Assert.Equal(25m, summary.RealizedCurrentYearAmount);
    }

    [Fact]
    public async Task Sorting_by_realized_amount_orders_contracts_in_both_directions()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Low", occurrences: [(YearStart, 10m)]);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "High", occurrences: [(YearStart, 90m)]);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "None");

        var ascending = await GetPageAsync(client, "/api/opex/contracts?sort=realizedCurrentYearAmount&sortDirection=asc");
        var descending = await GetPageAsync(client, "/api/opex/contracts?sort=realizedCurrentYearAmount&sortDirection=desc");

        Assert.Equal(new[] { "None", "Low", "High" }, ascending.Items.Select(item => item.Name).ToArray());
        Assert.Equal(new[] { "High", "Low", "None" }, descending.Items.Select(item => item.Name).ToArray());
    }

    private static async Task<PaginatedResponse<OpexContractSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<OpexContractSummaryResponse>>(
            route,
            CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code, IReadOnlyDictionary<string, string[]>? Errors);
}

using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Opex;

public sealed class OpexContractListTests
{
    [Fact]
    public async Task Contracts_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/opex/contracts", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Contracts_paginate_at_the_database_level_with_a_total_count()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        for (var index = 0; index < 7; index++)
        {
            await OpexTestData.SeedContractAsync(server.Services, founderId, name: $"Contract {index}");
        }

        var firstPage = await GetPageAsync(client, "/api/opex/contracts?page=1&pageSize=5");
        var secondPage = await GetPageAsync(client, "/api/opex/contracts?page=2&pageSize=5");
        var beyond = await GetPageAsync(client, "/api/opex/contracts?page=9&pageSize=5");

        Assert.Equal(7, firstPage.TotalCount);
        Assert.Equal(5, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.Equal(7, beyond.TotalCount);
        Assert.Empty(beyond.Items);
    }

    [Theory]
    [InlineData("/api/opex/contracts?page=0", "page")]
    [InlineData("/api/opex/contracts?pageSize=0", "pageSize")]
    [InlineData("/api/opex/contracts?pageSize=101", "pageSize")]
    public async Task Contracts_reject_out_of_range_pagination(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Default_order_is_name_ascending()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var alpha = await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Alpha");
        var beta = await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Beta");

        var page = await GetPageAsync(client, "/api/opex/contracts");

        Assert.Equal(new[] { alpha, beta }, page.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task Equal_sort_keys_break_ties_by_descending_id()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        // Contract names are globally unique, so the deterministic id tie-breaker is
        // exercised through a non-unique sort key: three contracts share a status.
        var first = await OpexTestData.SeedContractAsync(server.Services, founderId, name: "First", status: OpexContractStatus.Active);
        var second = await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Second", status: OpexContractStatus.Active);
        var third = await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Third", status: OpexContractStatus.Active);

        var page = await GetPageAsync(client, "/api/opex/contracts?sort=status&sortDirection=asc");

        // Within the shared status the higher id wins as the stable tie-breaker.
        Assert.Equal(new[] { third, second, first }, page.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task Contracts_sort_by_name_in_both_directions()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Banana");
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Apple");
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Cherry");

        var ascending = await GetPageAsync(client, "/api/opex/contracts?sort=name&sortDirection=asc");
        var descending = await GetPageAsync(client, "/api/opex/contracts?sort=name&sortDirection=desc");

        Assert.Equal(new[] { "Apple", "Banana", "Cherry" }, ascending.Items.Select(item => item.Name).ToArray());
        Assert.Equal(new[] { "Cherry", "Banana", "Apple" }, descending.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Contracts_sort_by_estimated_annual_amount_with_nulls_last()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Cheap", estimatedAnnualAmount: 5m);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Pricey", estimatedAnnualAmount: 50m);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Unknown", estimatedAnnualAmount: null);

        var ascending = await GetPageAsync(client, "/api/opex/contracts?sort=estimatedAnnualAmount&sortDirection=asc");
        var descending = await GetPageAsync(client, "/api/opex/contracts?sort=estimatedAnnualAmount&sortDirection=desc");

        Assert.Equal(new[] { "Cheap", "Pricey", "Unknown" }, ascending.Items.Select(item => item.Name).ToArray());
        Assert.Equal(new[] { "Pricey", "Cheap", "Unknown" }, descending.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Optional_supplier_sort_orders_nulls_last_in_both_directions()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "HasAmazon", supplierName: "Amazon");
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "HasIkea", supplierName: "IKEA");
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "NoSupplier", supplierName: null);

        var ascending = await GetPageAsync(client, "/api/opex/contracts?sort=supplier&sortDirection=asc");
        var descending = await GetPageAsync(client, "/api/opex/contracts?sort=supplier&sortDirection=desc");

        Assert.Equal(new[] { "HasAmazon", "HasIkea", "NoSupplier" }, ascending.Items.Select(item => item.Name).ToArray());
        Assert.Equal(new[] { "HasIkea", "HasAmazon", "NoSupplier" }, descending.Items.Select(item => item.Name).ToArray());
    }

    [Theory]
    [InlineData("/api/opex/contracts?sort=unknown", "sort")]
    [InlineData("/api/opex/contracts?sortDirection=sideways", "sortDirection")]
    [InlineData("/api/opex/contracts?type=Refund", "type")]
    [InlineData("/api/opex/contracts?status=Pending", "status")]
    [InlineData("/api/opex/contracts?frequency=Daily", "frequency")]
    [InlineData("/api/opex/contracts?visibility=Secret", "visibility")]
    public async Task Contracts_reject_invalid_query_values(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Exact_filters_narrow_the_result_set()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Income contract", movementType: OpexMovementType.Income);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Expense contract", movementType: OpexMovementType.Expense);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "On hold contract", status: OpexContractStatus.OnHold);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Weekly contract", frequency: OpexExpectedFrequency.Weekly);

        var income = await GetPageAsync(client, "/api/opex/contracts?type=Income");
        var onHold = await GetPageAsync(client, "/api/opex/contracts?status=OnHold");
        var weekly = await GetPageAsync(client, "/api/opex/contracts?frequency=Weekly");

        Assert.Equal("Income contract", Assert.Single(income.Items).Name);
        Assert.Equal("On hold contract", Assert.Single(onHold.Items).Name);
        Assert.Equal("Weekly contract", Assert.Single(weekly.Items).Name);
    }

    [Fact]
    public async Task Catalog_and_visibility_filters_each_narrow_the_result_set()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(
            server.Services,
            founderId,
            name: "Full",
            categoryName: "Other",
            supplierName: "Amazon",
            costCenterName: "Household",
            currencyCode: ConfigurationCatalog.CurrencyCodes.Euro,
            visibility: RecordVisibility.Public);
        await OpexTestData.SeedContractAsync(
            server.Services,
            founderId,
            name: "Sparse",
            categoryName: "Utilities",
            supplierName: null,
            costCenterName: null,
            currencyCode: ConfigurationCatalog.CurrencyCodes.UsDollar,
            visibility: RecordVisibility.Private);

        var utilitiesId = await OpexTestData.CategoryIdAsync(server.Services, "Utilities");
        var amazonId = await OpexTestData.SupplierIdAsync(server.Services, "Amazon");
        var householdId = await OpexTestData.CostCenterIdAsync(server.Services, "Household");
        var dollarId = await OpexTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.UsDollar);

        Assert.Equal("Sparse", Assert.Single((await GetPageAsync(client, $"/api/opex/contracts?category={utilitiesId}")).Items).Name);
        Assert.Equal("Full", Assert.Single((await GetPageAsync(client, $"/api/opex/contracts?supplier={amazonId}")).Items).Name);
        Assert.Equal("Full", Assert.Single((await GetPageAsync(client, $"/api/opex/contracts?costCenter={householdId}")).Items).Name);
        Assert.Equal("Sparse", Assert.Single((await GetPageAsync(client, $"/api/opex/contracts?currency={dollarId}")).Items).Name);
        Assert.Equal("Sparse", Assert.Single((await GetPageAsync(client, "/api/opex/contracts?visibility=Private")).Items).Name);
        Assert.Equal("Full", Assert.Single((await GetPageAsync(client, "/api/opex/contracts?visibility=Public")).Items).Name);
    }

    [Fact]
    public async Task Contracts_sort_by_status_and_category_names()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Planning", status: OpexContractStatus.Planning);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Active", status: OpexContractStatus.Active);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Closed", status: OpexContractStatus.Closed);

        var byStatus = await GetPageAsync(client, "/api/opex/contracts?sort=status&sortDirection=asc");

        // Status persists as its member name, so ascending orders Active < Closed < Planning.
        Assert.Equal(new[] { "Active", "Closed", "Planning" }, byStatus.Items.Select(item => item.Status).ToArray());
    }

    [Fact]
    public async Task Search_matches_name_and_notes_without_duplicating_contracts()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Widget rental");
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Plain name", notes: "Contains a widget reference");
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "Unrelated");

        var page = await GetPageAsync(client, "/api/opex/contracts?search=WIDGET");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.DoesNotContain(page.Items, item => item.Name == "Unrelated");
    }

    [Fact]
    public async Task Listing_hides_other_users_private_contracts_from_everyone_including_admins()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        await OpexTestData.SeedContractAsync(server.Services, memberId, name: "Member public");
        await OpexTestData.SeedContractAsync(server.Services, memberId, name: "Member private", visibility: RecordVisibility.Private);

        var adminView = await GetPageAsync(admin, "/api/opex/contracts");
        var memberView = await GetPageAsync(member, "/api/opex/contracts");

        Assert.Contains(adminView.Items, item => item.Name == "Member public");
        Assert.DoesNotContain(adminView.Items, item => item.Name == "Member private");
        Assert.Contains(memberView.Items, item => item.Name == "Member private");
    }

    [Fact]
    public async Task Creator_filter_returns_only_the_requested_authors_accessible_contracts()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("author", "AuthorPass123!");
        await OpexTestData.SeedContractAsync(server.Services, founderId, name: "By founder");
        await OpexTestData.SeedContractAsync(server.Services, memberId, name: "By author");

        var byAuthor = await GetPageAsync(admin, $"/api/opex/contracts?creator={memberId}");

        Assert.Equal("By author", Assert.Single(byAuthor.Items).Name);
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

    private sealed record ProblemPayload(IReadOnlyDictionary<string, string[]>? Errors);
}

using System.Net;
using System.Net.Http.Json;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Capex;

public sealed class CapexEntryListTests
{
    private static readonly DateOnly BaseDate = new(2026, 6, 14);

    [Fact]
    public async Task Entries_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/capex/entries", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Entries_paginate_at_the_database_level_with_a_total_count()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        for (var index = 0; index < 7; index++)
        {
            await CapexTestData.SeedEntryAsync(server.Services, founderId, title: $"Entry {index}");
        }

        var firstPage = await GetPageAsync(client, "/api/capex/entries?page=1&pageSize=5");
        var secondPage = await GetPageAsync(client, "/api/capex/entries?page=2&pageSize=5");
        var beyond = await GetPageAsync(client, "/api/capex/entries?page=9&pageSize=5");

        Assert.Equal(7, firstPage.TotalCount);
        Assert.Equal(5, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.Equal(7, beyond.TotalCount);
        Assert.Empty(beyond.Items);
    }

    [Theory]
    [InlineData("/api/capex/entries?page=0", "page")]
    [InlineData("/api/capex/entries?pageSize=0", "pageSize")]
    [InlineData("/api/capex/entries?pageSize=101", "pageSize")]
    public async Task Entries_reject_out_of_range_pagination(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Default_order_is_due_date_descending_with_id_tie_breaker()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var older = await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Older", dueDate: BaseDate.AddDays(-1));
        var firstSameDay = await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Same A", dueDate: BaseDate);
        var secondSameDay = await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Same B", dueDate: BaseDate);

        var page = await GetPageAsync(client, "/api/capex/entries");

        // Newest DueDate first; within the same DueDate the higher id wins.
        Assert.Equal(
            new[] { secondSameDay, firstSameDay, older },
            page.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task Entries_sort_by_title_in_both_directions()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Banana");
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Apple");
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Cherry");

        var ascending = await GetPageAsync(client, "/api/capex/entries?sort=title&sortDirection=asc");
        var descending = await GetPageAsync(client, "/api/capex/entries?sort=title&sortDirection=desc");

        Assert.Equal(new[] { "Apple", "Banana", "Cherry" }, ascending.Items.Select(item => item.Title).ToArray());
        Assert.Equal(new[] { "Cherry", "Banana", "Apple" }, descending.Items.Select(item => item.Title).ToArray());
    }

    [Fact]
    public async Task Entries_sort_by_total_amount()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Cheap", items: [new("Cheap", 1m, 5m)]);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Pricey", items: [new("Pricey", 1m, 50m)]);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Mid", items: [new("Mid", 1m, 25m)]);

        var page = await GetPageAsync(client, "/api/capex/entries?sort=total&sortDirection=asc");

        Assert.Equal(new[] { "Cheap", "Mid", "Pricey" }, page.Items.Select(item => item.Title).ToArray());
    }

    [Fact]
    public async Task Optional_supplier_sort_orders_nulls_last_in_both_directions()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "HasAmazon", supplierName: "Amazon");
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "HasIkea", supplierName: "IKEA");
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "NoSupplier", supplierName: null);

        var ascending = await GetPageAsync(client, "/api/capex/entries?sort=supplier&sortDirection=asc");
        var descending = await GetPageAsync(client, "/api/capex/entries?sort=supplier&sortDirection=desc");

        Assert.Equal(new[] { "HasAmazon", "HasIkea", "NoSupplier" }, ascending.Items.Select(item => item.Title).ToArray());
        Assert.Equal(new[] { "HasIkea", "HasAmazon", "NoSupplier" }, descending.Items.Select(item => item.Title).ToArray());
    }

    [Theory]
    [InlineData("/api/capex/entries?sort=unknown", "sort")]
    [InlineData("/api/capex/entries?sortDirection=sideways", "sortDirection")]
    [InlineData("/api/capex/entries?type=Refund", "type")]
    [InlineData("/api/capex/entries?status=Pending", "status")]
    [InlineData("/api/capex/entries?visibility=Secret", "visibility")]
    public async Task Entries_reject_invalid_query_values(string route, string field)
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
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Income entry", movementType: CapexMovementType.Income);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Expense entry", movementType: CapexMovementType.Expense);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Completed entry", status: CapexEntryStatus.Completed);

        var income = await GetPageAsync(client, "/api/capex/entries?type=Income");
        var completed = await GetPageAsync(client, "/api/capex/entries?status=Completed");

        Assert.Equal("Income entry", Assert.Single(income.Items).Title);
        Assert.Equal("Completed entry", Assert.Single(completed.Items).Title);
    }

    [Fact]
    public async Task Catalog_and_visibility_filters_each_narrow_the_result_set()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(
            server.Services,
            founderId,
            title: "Full",
            categoryName: "Other",
            supplierName: "Amazon",
            costCenterName: "Household",
            currencyCode: ConfigurationCatalog.CurrencyCodes.Euro,
            visibility: RecordVisibility.Public);
        await CapexTestData.SeedEntryAsync(
            server.Services,
            founderId,
            title: "Sparse",
            categoryName: "Furniture",
            supplierName: null,
            costCenterName: null,
            currencyCode: ConfigurationCatalog.CurrencyCodes.UsDollar,
            visibility: RecordVisibility.Private);

        var furnitureId = await CapexTestData.CategoryIdAsync(server.Services, "Furniture");
        var amazonId = await CapexTestData.SupplierIdAsync(server.Services, "Amazon");
        var householdId = await CapexTestData.CostCenterIdAsync(server.Services, "Household");
        var dollarId = await CapexTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.UsDollar);

        Assert.Equal("Sparse", Assert.Single((await GetPageAsync(client, $"/api/capex/entries?category={furnitureId}")).Items).Title);
        Assert.Equal("Full", Assert.Single((await GetPageAsync(client, $"/api/capex/entries?supplier={amazonId}")).Items).Title);
        Assert.Equal("Full", Assert.Single((await GetPageAsync(client, $"/api/capex/entries?costCenter={householdId}")).Items).Title);
        Assert.Equal("Sparse", Assert.Single((await GetPageAsync(client, $"/api/capex/entries?currency={dollarId}")).Items).Title);
        Assert.Equal("Sparse", Assert.Single((await GetPageAsync(client, "/api/capex/entries?visibility=Private")).Items).Title);
        Assert.Equal("Full", Assert.Single((await GetPageAsync(client, "/api/capex/entries?visibility=Public")).Items).Title);
    }

    [Fact]
    public async Task Entries_sort_by_status_and_category_names()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Planning", status: CapexEntryStatus.Planning);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Completed", status: CapexEntryStatus.Completed);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Canceled", status: CapexEntryStatus.Canceled);

        var byStatus = await GetPageAsync(client, "/api/capex/entries?sort=status&sortDirection=asc");

        // Status persists as its member name, so ascending orders Canceled < Completed < Planning.
        Assert.Equal(new[] { "Canceled", "Completed", "Planning" }, byStatus.Items.Select(item => item.Status).ToArray());
    }

    [Fact]
    public async Task Due_date_bounds_are_inclusive()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Before", dueDate: BaseDate.AddDays(-1));
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "OnBound", dueDate: BaseDate);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "After", dueDate: BaseDate.AddDays(1));

        var bounded = await GetPageAsync(
            client,
            $"/api/capex/entries?from={BaseDate:yyyy-MM-dd}&to={BaseDate:yyyy-MM-dd}");

        Assert.Equal("OnBound", Assert.Single(bounded.Items).Title);
    }

    [Fact]
    public async Task Search_matches_title_notes_and_item_descriptions_without_duplicating_entries()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Widget order", items: [new("Plain", 1m, 1m)]);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Plain title", notes: "Contains a widget reference");
        await CapexTestData.SeedEntryAsync(
            server.Services,
            founderId,
            title: "Two matching items",
            items: [new("First widget", 1m, 1m), new("Second widget", 1m, 1m)]);
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "Unrelated", items: [new("Nothing", 1m, 1m)]);

        var page = await GetPageAsync(client, "/api/capex/entries?search=WIDGET");

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(3, page.Items.Count);
        Assert.DoesNotContain(page.Items, item => item.Title == "Unrelated");
    }

    [Fact]
    public async Task Listing_hides_other_users_private_entries_from_everyone_including_admins()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        await CapexTestData.SeedEntryAsync(server.Services, memberId, title: "Member public");
        await CapexTestData.SeedEntryAsync(server.Services, memberId, title: "Member private", visibility: RecordVisibility.Private);

        var adminView = await GetPageAsync(admin, "/api/capex/entries");
        var memberView = await GetPageAsync(member, "/api/capex/entries");

        // The bootstrap administrator sees the public entry but not the member's private one.
        Assert.Contains(adminView.Items, item => item.Title == "Member public");
        Assert.DoesNotContain(adminView.Items, item => item.Title == "Member private");
        // The owning member sees both of their entries.
        Assert.Contains(memberView.Items, item => item.Title == "Member private");
    }

    [Fact]
    public async Task Creator_filter_returns_only_the_requested_authors_accessible_entries()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("author", "AuthorPass123!");
        await CapexTestData.SeedEntryAsync(server.Services, founderId, title: "By founder");
        await CapexTestData.SeedEntryAsync(server.Services, memberId, title: "By author");

        var byAuthor = await GetPageAsync(admin, $"/api/capex/entries?creator={memberId}");

        Assert.Equal("By author", Assert.Single(byAuthor.Items).Title);
    }

    private static async Task<PaginatedResponse<CapexEntrySummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<CapexEntrySummaryResponse>>(
            route,
            CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(IReadOnlyDictionary<string, string[]>? Errors);
}

using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryItemListTests
{
    [Fact]
    public async Task Items_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/inventory/items", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Items_paginate_at_the_database_level_with_a_total_count()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        for (var index = 0; index < 7; index++)
        {
            await InventoryTestData.SeedItemAsync(server.Services, founderId, name: $"Item {index}");
        }

        var firstPage = await GetPageAsync(client, "/api/inventory/items?page=1&pageSize=5");
        var secondPage = await GetPageAsync(client, "/api/inventory/items?page=2&pageSize=5");
        var beyond = await GetPageAsync(client, "/api/inventory/items?page=9&pageSize=5");

        Assert.Equal(7, firstPage.TotalCount);
        Assert.Equal(5, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.Equal(7, beyond.TotalCount);
        Assert.Empty(beyond.Items);
    }

    [Theory]
    [InlineData("/api/inventory/items?page=0", "page")]
    [InlineData("/api/inventory/items?pageSize=0", "pageSize")]
    [InlineData("/api/inventory/items?pageSize=101", "pageSize")]
    public async Task Items_reject_out_of_range_pagination(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Theory]
    [InlineData("/api/inventory/items?status=Nope", "status")]
    [InlineData("/api/inventory/items?visibility=Nope", "visibility")]
    [InlineData("/api/inventory/items?sort=unknown", "sort")]
    [InlineData("/api/inventory/items?sortDirection=sideways", "sortDirection")]
    public async Task Items_reject_invalid_query_values(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Default_order_is_name_then_id_ascending()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var alpha = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Alpha");
        var beta = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Beta");

        var page = await GetPageAsync(client, "/api/inventory/items");

        Assert.Equal(new[] { alpha, beta }, page.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task Equal_sort_keys_break_ties_by_ascending_id()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        // Three items share the Active status, so the deterministic id tie-breaker is
        // exercised through a non-unique sort key.
        var first = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "First", status: InventoryItemStatus.Active);
        var second = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Second", status: InventoryItemStatus.Active);
        var third = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Third", status: InventoryItemStatus.Active);

        var page = await GetPageAsync(client, "/api/inventory/items?sort=status&sortDirection=asc");

        Assert.Equal(new[] { first, second, third }, page.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task Items_sort_by_name_in_both_directions()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Banana");
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Apple");
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Cherry");

        var ascending = await GetPageAsync(client, "/api/inventory/items?sort=name&sortDirection=asc");
        var descending = await GetPageAsync(client, "/api/inventory/items?sort=name&sortDirection=desc");

        Assert.Equal(new[] { "Apple", "Banana", "Cherry" }, ascending.Items.Select(item => item.Name).ToArray());
        Assert.Equal(new[] { "Cherry", "Banana", "Apple" }, descending.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Items_sort_by_category_and_location_names()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Cleaner", categoryName: "Cleaning", locationName: "Pantry");
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Apple", categoryName: "Food", locationName: "Fridge");
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Pills", categoryName: "Medicine", locationName: "Bathroom");

        var byCategory = await GetPageAsync(client, "/api/inventory/items?sort=category&sortDirection=asc");
        var byLocation = await GetPageAsync(client, "/api/inventory/items?sort=location&sortDirection=asc");

        Assert.Equal(new[] { "Cleaning", "Food", "Medicine" }, byCategory.Items.Select(item => item.CategoryName).ToArray());
        Assert.Equal(new[] { "Bathroom", "Fridge", "Pantry" }, byLocation.Items.Select(item => item.LocationName).ToArray());
    }

    [Fact]
    public async Task Items_sort_by_current_stock()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Low", currentStock: 1m);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "High", currentStock: 9m);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Mid", currentStock: 5m);

        var ascending = await GetPageAsync(client, "/api/inventory/items?sort=currentStock&sortDirection=asc");

        Assert.Equal(new[] { "Low", "Mid", "High" }, ascending.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Search_matches_name_and_notes_without_duplicating_items()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Widget pack");
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Plain name", notes: "Contains a widget reference");
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Unrelated");

        var page = await GetPageAsync(client, "/api/inventory/items?search=WIDGET");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.DoesNotContain(page.Items, item => item.Name == "Unrelated");
    }

    [Fact]
    public async Task Status_category_location_and_visibility_filters_are_exact()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Active food", status: InventoryItemStatus.Active, categoryName: "Food", locationName: "Fridge");
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Candidate pets", status: InventoryItemStatus.Candidate, categoryName: "Pets", locationName: "Pantry", visibility: RecordVisibility.Private);

        var foodId = await InventoryTestData.CategoryIdAsync(server.Services, "Food");
        var fridgeId = await InventoryTestData.LocationIdAsync(server.Services, "Fridge");

        Assert.Equal("Active food", Assert.Single((await GetPageAsync(client, "/api/inventory/items?status=Active")).Items).Name);
        Assert.Equal("Candidate pets", Assert.Single((await GetPageAsync(client, "/api/inventory/items?status=Candidate")).Items).Name);
        Assert.Equal("Active food", Assert.Single((await GetPageAsync(client, $"/api/inventory/items?category={foodId}")).Items).Name);
        Assert.Equal("Active food", Assert.Single((await GetPageAsync(client, $"/api/inventory/items?location={fridgeId}")).Items).Name);
        Assert.Equal("Candidate pets", Assert.Single((await GetPageAsync(client, "/api/inventory/items?visibility=Private")).Items).Name);
        Assert.Equal("Active food", Assert.Single((await GetPageAsync(client, "/api/inventory/items?visibility=Public")).Items).Name);
    }

    [Fact]
    public async Task Supplier_filter_returns_only_items_allowed_for_that_supplier()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "From Amazon", supplierNames: ["Amazon"]);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "From IKEA", supplierNames: ["IKEA"]);
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "From both", supplierNames: ["Amazon", "IKEA"]);

        var amazonId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");

        var byAmazon = await GetPageAsync(client, $"/api/inventory/items?supplier={amazonId}");

        Assert.Equal(2, byAmazon.TotalCount);
        Assert.Equal(new[] { "From Amazon", "From both" }, byAmazon.Items.Select(item => item.Name).OrderBy(name => name).ToArray());
    }

    [Fact]
    public async Task Listing_hides_other_users_private_items_from_everyone_including_admins()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        await InventoryTestData.SeedItemAsync(server.Services, memberId, name: "Member public");
        await InventoryTestData.SeedItemAsync(server.Services, memberId, name: "Member private", visibility: RecordVisibility.Private);

        var adminView = await GetPageAsync(admin, "/api/inventory/items");
        var memberView = await GetPageAsync(member, "/api/inventory/items");

        Assert.Contains(adminView.Items, item => item.Name == "Member public");
        Assert.DoesNotContain(adminView.Items, item => item.Name == "Member private");
        Assert.Contains(memberView.Items, item => item.Name == "Member private");
    }

    [Fact]
    public async Task Creator_filter_returns_only_the_requested_authors_accessible_items()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("author", "AuthorPass123!");
        await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "By founder");
        await InventoryTestData.SeedItemAsync(server.Services, memberId, name: "By author");

        var byAuthor = await GetPageAsync(admin, $"/api/inventory/items?creator={memberId}");

        Assert.Equal("By author", Assert.Single(byAuthor.Items).Name);
    }

    private static async Task<PaginatedResponse<InventoryItemSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<InventoryItemSummaryResponse>>(
            route,
            CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(IReadOnlyDictionary<string, string[]>? Errors);
}

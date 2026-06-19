using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Assets;

public sealed class AssetListTests
{
    [Fact]
    public async Task Assets_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/assets/items", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assets_paginate_at_the_database_level_with_a_total_count()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        for (var index = 0; index < 7; index++)
        {
            await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: $"Asset {index}");
        }

        var firstPage = await GetPageAsync(client, "/api/assets/items?page=1&pageSize=5");
        var secondPage = await GetPageAsync(client, "/api/assets/items?page=2&pageSize=5");
        var beyond = await GetPageAsync(client, "/api/assets/items?page=9&pageSize=5");

        Assert.Equal(7, firstPage.TotalCount);
        Assert.Equal(5, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.Empty(beyond.Items);
    }

    [Theory]
    [InlineData("/api/assets/items?page=0", "page")]
    [InlineData("/api/assets/items?pageSize=0", "pageSize")]
    [InlineData("/api/assets/items?pageSize=101", "pageSize")]
    public async Task Assets_reject_out_of_range_pagination(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Theory]
    [InlineData("/api/assets/items?sort=unknown", "sort")]
    [InlineData("/api/assets/items?sortDirection=sideways", "sortDirection")]
    [InlineData("/api/assets/items?status=Broken", "status")]
    [InlineData("/api/assets/items?visibility=Secret", "visibility")]
    public async Task Assets_reject_invalid_query_values(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Default_order_is_name_then_ascending_id()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var alpha = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Alpha");
        var beta = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Beta");

        var page = await GetPageAsync(client, "/api/assets/items");

        Assert.Equal(new[] { alpha, beta }, page.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task Equal_sort_keys_break_ties_by_ascending_id()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        // Three assets share a status, so the deterministic id tie-breaker decides the
        // order within the shared sort key; the documented tie-breaker is ascending.
        var first = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "First", status: AssetStatus.Active);
        var second = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Second", status: AssetStatus.Active);
        var third = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Third", status: AssetStatus.Active);

        var page = await GetPageAsync(client, "/api/assets/items?sort=status&sortDirection=asc");

        Assert.Equal(new[] { first, second, third }, page.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task Assets_sort_by_name_in_both_directions()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Banana");
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Apple");
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Cherry");

        var ascending = await GetPageAsync(client, "/api/assets/items?sort=name&sortDirection=asc");
        var descending = await GetPageAsync(client, "/api/assets/items?sort=name&sortDirection=desc");

        Assert.Equal(new[] { "Apple", "Banana", "Cherry" }, ascending.Items.Select(item => item.Name).ToArray());
        Assert.Equal(new[] { "Cherry", "Banana", "Apple" }, descending.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Optional_expected_end_of_life_sorts_nulls_last_in_both_directions()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Soon", expectedEndOfLifeDate: new DateOnly(2026, 6, 1));
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Later", expectedEndOfLifeDate: new DateOnly(2027, 6, 1));
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Never", expectedEndOfLifeDate: null);

        var ascending = await GetPageAsync(client, "/api/assets/items?sort=expectedEndOfLife&sortDirection=asc");
        var descending = await GetPageAsync(client, "/api/assets/items?sort=expectedEndOfLife&sortDirection=desc");

        Assert.Equal(new[] { "Soon", "Later", "Never" }, ascending.Items.Select(item => item.Name).ToArray());
        Assert.Equal(new[] { "Later", "Soon", "Never" }, descending.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Exact_filters_each_narrow_the_result_set()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Sofa", categoryName: "Furniture", locationName: "Living room", status: AssetStatus.Active, visibility: RecordVisibility.Public);
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Drill", categoryName: "Tools", locationName: "Garage", status: AssetStatus.Stored, visibility: RecordVisibility.Private);

        var furnitureId = await AssetsTestData.CategoryIdAsync(server.Services, "Furniture");
        var garageId = await AssetsTestData.LocationIdAsync(server.Services, "Garage");

        Assert.Equal("Sofa", Assert.Single((await GetPageAsync(client, $"/api/assets/items?category={furnitureId}")).Items).Name);
        Assert.Equal("Drill", Assert.Single((await GetPageAsync(client, $"/api/assets/items?location={garageId}")).Items).Name);
        Assert.Equal("Drill", Assert.Single((await GetPageAsync(client, "/api/assets/items?status=Stored")).Items).Name);
        Assert.Equal("Drill", Assert.Single((await GetPageAsync(client, "/api/assets/items?visibility=Private")).Items).Name);
        Assert.Equal("Sofa", Assert.Single((await GetPageAsync(client, "/api/assets/items?visibility=Public")).Items).Name);
    }

    [Fact]
    public async Task Search_matches_identification_fields_and_notes_without_duplicating_assets()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Widget machine");
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Plain", code: "WIDGET-01");
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Branded", brandModel: "Widget Pro");
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Noted", notes: "contains a widget inside");
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Unrelated");

        var page = await GetPageAsync(client, "/api/assets/items?search=WIDGET");

        Assert.Equal(4, page.TotalCount);
        Assert.Equal(4, page.Items.Count);
        Assert.DoesNotContain(page.Items, item => item.Name == "Unrelated");
    }

    [Fact]
    public async Task Listing_hides_other_users_private_assets_from_everyone_including_admins()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        await AssetsTestData.SeedAssetAsync(server.Services, memberId, name: "Member public");
        await AssetsTestData.SeedAssetAsync(server.Services, memberId, name: "Member private", visibility: RecordVisibility.Private);

        var adminView = await GetPageAsync(admin, "/api/assets/items");
        var memberView = await GetPageAsync(member, "/api/assets/items");

        Assert.Contains(adminView.Items, item => item.Name == "Member public");
        Assert.DoesNotContain(adminView.Items, item => item.Name == "Member private");
        Assert.Contains(memberView.Items, item => item.Name == "Member private");
    }

    [Fact]
    public async Task Creator_filter_returns_only_the_requested_authors_accessible_assets()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("author", "AuthorPass123!");
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "By founder");
        await AssetsTestData.SeedAssetAsync(server.Services, memberId, name: "By author");

        var byAuthor = await GetPageAsync(admin, $"/api/assets/items?creator={memberId}");

        Assert.Equal("By author", Assert.Single(byAuthor.Items).Name);
    }

    [Fact]
    public async Task Summary_carries_resolved_catalog_names_and_a_placeholder_thumbnail()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Lamp", categoryName: "Furniture", locationName: "Office");

        var item = Assert.Single((await GetPageAsync(client, "/api/assets/items")).Items);

        Assert.Equal("Furniture", item.CategoryName);
        Assert.Equal("Office", item.LocationName);
        Assert.Equal("placeholder", item.Thumbnail.Source);
        Assert.Null(item.Thumbnail.AttachmentId);
        Assert.Equal(CapexTestServer.AdminUserName, item.CreatorName);
    }

    private static async Task<PaginatedResponse<AssetSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<AssetSummaryResponse>>(
            route,
            CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(IReadOnlyDictionary<string, string[]>? Errors);
}

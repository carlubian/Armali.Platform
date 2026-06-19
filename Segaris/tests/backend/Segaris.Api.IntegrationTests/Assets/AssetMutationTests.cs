using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Assets;

public sealed class AssetMutationTests
{
    [Fact]
    public async Task Create_persists_the_asset_with_creation_defaults()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (categoryId, locationId) = await CatalogAsync(server, "Furniture", "Bedroom");
        var request = new CreateAssetRequest(
            "  Nightstand  ", categoryId, locationId,
            Status: null, Code: null, BrandModel: null, SerialNumber: null,
            AcquisitionDate: null, ExpectedEndOfLifeDate: null, Notes: null, Visibility: null);

        using var response = await CapexApi.PostJsonAsync(client, "/api/assets/items", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AssetResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Nightstand", created.Name);
        Assert.Equal("Active", created.Status);
        Assert.Equal("Public", created.Visibility);
        Assert.Null(created.Code);
        Assert.Empty(created.Attachments);
        Assert.Equal(CapexTestServer.AdminUserName, created.CreatedByName);

        var fetched = await client.GetFromJsonAsync<AssetResponse>($"/api/assets/items/{created.Id}", CancellationToken.None);
        Assert.Equal("Nightstand", fetched!.Name);
    }

    [Fact]
    public async Task Create_without_an_antiforgery_token_is_rejected()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var (categoryId, locationId) = await CatalogAsync(server, "Other", "Other");
        var request = NewAsset("Untokened", categoryId, locationId);

        using var response = await CapexApi.PostJsonAsync(client, "/api/assets/items", request, csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_a_blank_name()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (categoryId, locationId) = await CatalogAsync(server, "Other", "Other");
        var request = NewAsset("   ", categoryId, locationId);

        using var response = await CapexApi.PostJsonAsync(client, "/api/assets/items", request, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("assets.asset.validation", problem!.Code);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_catalog_reference()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (_, locationId) = await CatalogAsync(server, "Other", "Other");
        var request = NewAsset("Orphan", 999_999, locationId);

        using var response = await CapexApi.PostJsonAsync(client, "/api/assets/items", request, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("assets.catalog.unknown_reference", problem!.Code);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_code_ignoring_case_and_whitespace()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (categoryId, locationId) = await CatalogAsync(server, "Other", "Other");
        await CapexApi.PostJsonAsync(client, "/api/assets/items", NewAsset("First", categoryId, locationId, code: "INV-100"), csrf);

        var duplicate = NewAsset("Second", categoryId, locationId, code: "  inv-100  ");
        using var response = await CapexApi.PostJsonAsync(client, "/api/assets/items", duplicate, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("assets.asset.duplicate_code", problem!.Code);
    }

    [Fact]
    public async Task Multiple_assets_without_a_code_do_not_collide()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (categoryId, locationId) = await CatalogAsync(server, "Other", "Other");

        using var first = await CapexApi.PostJsonAsync(client, "/api/assets/items", NewAsset("No code A", categoryId, locationId), csrf);
        using var second = await CapexApi.PostJsonAsync(client, "/api/assets/items", NewAsset("No code B", categoryId, locationId), csrf);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Update_replaces_every_editable_field()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Original");
        var (categoryId, locationId) = await CatalogAsync(server, "Electronics", "Office");

        var update = new UpdateAssetRequest(
            "Revised", categoryId, locationId,
            Status: "Stored", Code: "E-9", BrandModel: "Dell", SerialNumber: "S1",
            AcquisitionDate: new DateOnly(2025, 1, 1), ExpectedEndOfLifeDate: new DateOnly(2031, 1, 1),
            Notes: "Updated", Visibility: "Public");

        using var response = await CapexApi.PutJsonAsync(client, $"/api/assets/items/{assetId}", update, csrf);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AssetResponse>(CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Revised", updated.Name);
        Assert.Equal("Electronics", updated.CategoryName);
        Assert.Equal("Office", updated.LocationName);
        Assert.Equal("Stored", updated.Status);
        Assert.Equal("E-9", updated.Code);
        Assert.Equal("Dell", updated.BrandModel);
        Assert.Equal("Updated", updated.Notes);
    }

    [Fact]
    public async Task Update_of_a_missing_asset_returns_an_asset_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (categoryId, locationId) = await CatalogAsync(server, "Other", "Other");
        var update = ReplaceAsset("Ghost", categoryId, locationId);

        using var response = await CapexApi.PutJsonAsync(client, "/api/assets/items/999999", update, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("assets.asset.not_found", problem!.Code);
    }

    [Fact]
    public async Task Any_user_may_edit_a_public_asset_created_by_another()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Shared", visibility: RecordVisibility.Public);

        await server.CreateUserAsync("collaborator", "CollaboratorPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "collaborator", "CollaboratorPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var (categoryId, locationId) = await CatalogAsync(server, "Other", "Other");

        var update = ReplaceAsset("Shared", categoryId, locationId, status: "Stored", visibility: "Public");
        using var response = await CapexApi.PutJsonAsync(member, $"/api/assets/items/{assetId}", update, memberCsrf);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Only_the_creator_may_change_visibility()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Owned", visibility: RecordVisibility.Public);

        await server.CreateUserAsync("intruder", "IntruderPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "intruder", "IntruderPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var (categoryId, locationId) = await CatalogAsync(server, "Other", "Other");

        var update = ReplaceAsset("Owned", categoryId, locationId, visibility: "Private");
        using var response = await CapexApi.PutJsonAsync(member, $"/api/assets/items/{assetId}", update, memberCsrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("assets.asset.visibility_forbidden", problem!.Code);
    }

    [Fact]
    public async Task Another_users_private_asset_cannot_be_updated_and_is_indistinguishable_from_missing()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        var memberId = await server.CreateUserAsync("owner", "OwnerPass123!");
        var privateId = await AssetsTestData.SeedAssetAsync(
            server.Services, memberId, name: "Owner private", visibility: RecordVisibility.Private);
        var (categoryId, locationId) = await CatalogAsync(server, "Other", "Other");

        var update = ReplaceAsset("Hijacked", categoryId, locationId);
        using var response = await CapexApi.PutJsonAsync(admin, $"/api/assets/items/{privateId}", update, adminCsrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("assets.asset.not_found", problem!.Code);
    }

    [Fact]
    public async Task Delete_removes_the_asset_and_makes_it_unreachable()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Disposable");

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/assets/items/{assetId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var fetch = await client.GetAsync($"/api/assets/items/{assetId}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_of_a_missing_asset_returns_an_asset_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.DeleteAsync(client, "/api/assets/items/999999", csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("assets.asset.not_found", problem!.Code);
    }

    private static async Task<(int CategoryId, int LocationId)> CatalogAsync(
        CapexTestServer server, string categoryName, string locationName) =>
        (await AssetsTestData.CategoryIdAsync(server.Services, categoryName),
            await AssetsTestData.LocationIdAsync(server.Services, locationName));

    private static CreateAssetRequest NewAsset(
        string? name, int categoryId, int locationId, string? code = null) =>
        new(name, categoryId, locationId,
            Status: null, Code: code, BrandModel: null, SerialNumber: null,
            AcquisitionDate: null, ExpectedEndOfLifeDate: null, Notes: null, Visibility: null);

    private static UpdateAssetRequest ReplaceAsset(
        string? name, int categoryId, int locationId, string? status = null, string? visibility = null) =>
        new(name, categoryId, locationId,
            Status: status, Code: null, BrandModel: null, SerialNumber: null,
            AcquisitionDate: null, ExpectedEndOfLifeDate: null, Notes: null, Visibility: visibility);

    private sealed record ProblemPayload(string? Code);
}

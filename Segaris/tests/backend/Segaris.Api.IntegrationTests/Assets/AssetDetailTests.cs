using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Assets;

public sealed class AssetDetailTests
{
    [Fact]
    public async Task Detail_projects_the_full_asset_with_resolved_names()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Workbench",
            categoryName: "Tools",
            locationName: "Garage",
            code: "WB-1",
            brandModel: "Acme 200",
            serialNumber: "SN-42",
            acquisitionDate: new DateOnly(2024, 3, 1),
            expectedEndOfLifeDate: new DateOnly(2030, 3, 1),
            notes: "Sturdy");

        var asset = await client.GetFromJsonAsync<AssetResponse>($"/api/assets/items/{assetId}", CancellationToken.None);

        Assert.NotNull(asset);
        Assert.Equal("Workbench", asset.Name);
        Assert.Equal("WB-1", asset.Code);
        Assert.Equal("Tools", asset.CategoryName);
        Assert.Equal("Garage", asset.LocationName);
        Assert.Equal("Active", asset.Status);
        Assert.Equal("Acme 200", asset.BrandModel);
        Assert.Equal("SN-42", asset.SerialNumber);
        Assert.Equal(new DateOnly(2024, 3, 1), asset.AcquisitionDate);
        Assert.Equal(new DateOnly(2030, 3, 1), asset.ExpectedEndOfLifeDate);
        Assert.Equal("Sturdy", asset.Notes);
        Assert.Equal("Public", asset.Visibility);
        Assert.Equal("placeholder", asset.Thumbnail.Source);
        Assert.Empty(asset.Attachments);
        Assert.Equal(CapexTestServer.AdminUserName, asset.CreatedByName);
    }

    [Fact]
    public async Task Detail_of_a_missing_asset_returns_an_asset_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync("/api/assets/items/999999", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("assets.asset.not_found", problem!.Code);
    }

    [Fact]
    public async Task Another_users_private_asset_is_indistinguishable_from_missing()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("owner", "OwnerPass123!");
        var privateId = await AssetsTestData.SeedAssetAsync(
            server.Services, memberId, name: "Owner private", visibility: RecordVisibility.Private);

        using var response = await admin.GetAsync($"/api/assets/items/{privateId}", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("assets.asset.not_found", problem!.Code);
    }

    private sealed record ProblemPayload(string? Code);
}

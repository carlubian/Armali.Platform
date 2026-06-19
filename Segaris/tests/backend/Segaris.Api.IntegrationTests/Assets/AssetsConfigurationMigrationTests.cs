using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Assets;

public sealed class AssetsConfigurationMigrationTests
{
    [Fact]
    public async Task Category_replacement_migrates_public_and_private_assets_and_audits_the_admin()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-asset-category-owner", "MemberPass123!");
        var publicAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services, adminId, name: "Public sofa", categoryName: "Furniture");
        var privateAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services, memberId, name: "Private sofa", categoryName: "Furniture", visibility: RecordVisibility.Private);
        var sourceId = await AssetsTestData.CategoryIdAsync(server.Services, "Furniture");
        var replacementId = await AssetsTestData.CategoryIdAsync(server.Services, "Tools");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/assets/categories/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var assets = await database.Set<Asset>()
            .Where(asset => asset.Id == publicAssetId || asset.Id == privateAssetId)
            .OrderBy(asset => asset.Id)
            .ToArrayAsync();
        Assert.All(assets, asset =>
        {
            Assert.Equal(replacementId, asset.CategoryId);
            Assert.Equal(adminId, asset.UpdatedBy);
            Assert.Equal(TimeSpan.Zero, asset.UpdatedAt.Offset);
        });
        Assert.False(await database.Set<AssetCategory>().AnyAsync(category => category.Id == sourceId));
    }

    [Fact]
    public async Task Location_replacement_migrates_public_and_private_assets()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-asset-location-owner", "MemberPass123!");
        var publicAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services, adminId, name: "Public drill", locationName: "Garage");
        var privateAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services, memberId, name: "Private drill", locationName: "Garage", visibility: RecordVisibility.Private);
        var sourceId = await AssetsTestData.LocationIdAsync(server.Services, "Garage");
        var replacementId = await AssetsTestData.LocationIdAsync(server.Services, "Storage room");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/assets/locations/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var assets = await database.Set<Asset>()
            .Where(asset => asset.Id == publicAssetId || asset.Id == privateAssetId)
            .ToArrayAsync();
        Assert.All(assets, asset =>
        {
            Assert.Equal(replacementId, asset.LocationId);
            Assert.Equal(adminId, asset.UpdatedBy);
        });
        Assert.False(await database.Set<AssetLocation>().AnyAsync(location => location.Id == sourceId));
    }

    [Fact]
    public async Task Category_clearing_is_rejected_and_leaves_references_unchanged()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(
            server.Services, adminId, name: "Required category", categoryName: "Furniture");
        var sourceId = await AssetsTestData.CategoryIdAsync(server.Services, "Furniture");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/assets/categories/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
            csrf);

        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("assets.category.invalid_replacement", problem!.Code);

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var asset = await database.Set<Asset>().SingleAsync(value => value.Id == assetId);
        Assert.Equal(sourceId, asset.CategoryId);
        Assert.True(await database.Set<AssetCategory>().AnyAsync(category => category.Id == sourceId));
    }

    [Fact]
    public async Task Impact_reports_private_references_without_disclosing_assets()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync("private-asset-impact", "MemberPass123!");
        await AssetsTestData.SeedAssetAsync(
            server.Services,
            memberId,
            name: "Private garage asset",
            locationName: "Garage",
            visibility: RecordVisibility.Private);
        var sourceId = await AssetsTestData.LocationIdAsync(server.Services, "Garage");
        using var client = await server.CreateAuthenticatedClientAsync();

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/assets/locations/{sourceId}/deletion-impact",
            CancellationToken.None);

        Assert.True(impact!.IsReferenced);
        Assert.False(impact.CanDeleteDirectly);
        Assert.False(impact.CanClearReferences);
        Assert.False(impact.RequiresExchangeRate);
        Assert.True(impact.HasReplacementCandidates);
    }

    private sealed record ProblemPayload(string? Code);
}

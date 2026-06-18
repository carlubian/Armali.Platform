using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Clothes;

public sealed class ClothesConfigurationMigrationTests
{
    [Fact]
    public async Task Category_replacement_migrates_public_and_private_garments_and_audits_the_admin()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-clothes-category-owner", "MemberPass123!");
        var publicGarmentId = await ClothesTestData.SeedGarmentAsync(
            server.Services, adminId, name: "Public tee", categoryName: "Tops");
        var privateGarmentId = await ClothesTestData.SeedGarmentAsync(
            server.Services, memberId, name: "Private tee", categoryName: "Tops", visibility: RecordVisibility.Private);
        var sourceId = await ClothesTestData.CategoryIdAsync(server.Services, "Tops");
        var replacementId = await ClothesTestData.CategoryIdAsync(server.Services, "Other");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/clothes/categories/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var garments = await database.Set<ClothesGarment>()
            .Where(garment => garment.Id == publicGarmentId || garment.Id == privateGarmentId)
            .OrderBy(garment => garment.Id)
            .ToArrayAsync();
        Assert.All(garments, garment =>
        {
            Assert.Equal(replacementId, garment.CategoryId);
            Assert.Equal(adminId, garment.UpdatedBy);
            Assert.Equal(TimeSpan.Zero, garment.UpdatedAt.Offset);
        });
        Assert.False(await database.Set<ClothingCategory>().AnyAsync(category => category.Id == sourceId));
    }

    [Fact]
    public async Task Colour_replacement_deduplicates_public_and_private_garment_associations()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-clothes-colour-owner", "MemberPass123!");
        var publicGarmentId = await ClothesTestData.SeedGarmentAsync(
            server.Services, adminId, name: "Public monochrome", colorNames: ["Black", "White"]);
        var privateGarmentId = await ClothesTestData.SeedGarmentAsync(
            server.Services, memberId, name: "Private monochrome", colorNames: ["Black"], visibility: RecordVisibility.Private);
        var sourceId = await ClothesTestData.ColorIdAsync(server.Services, "Black");
        var replacementId = await ClothesTestData.ColorIdAsync(server.Services, "White");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/clothes/colors/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal([replacementId], await ClothesTestData.GarmentColorIdsAsync(server.Services, publicGarmentId));
        Assert.Equal([replacementId], await ClothesTestData.GarmentColorIdsAsync(server.Services, privateGarmentId));

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var garments = await database.Set<ClothesGarment>()
            .Where(garment => garment.Id == publicGarmentId || garment.Id == privateGarmentId)
            .ToArrayAsync();
        Assert.All(garments, garment => Assert.Equal(adminId, garment.UpdatedBy));
        Assert.False(await database.Set<ClothingColor>().AnyAsync(color => color.Id == sourceId));
    }

    [Fact]
    public async Task Colour_references_can_be_cleared_on_public_and_private_garments()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-clothes-clear-owner", "MemberPass123!");
        var publicGarmentId = await ClothesTestData.SeedGarmentAsync(
            server.Services, adminId, name: "Public black garment", colorNames: ["Black"]);
        var privateGarmentId = await ClothesTestData.SeedGarmentAsync(
            server.Services, memberId, name: "Private black garment", colorNames: ["Black"], visibility: RecordVisibility.Private);
        var sourceId = await ClothesTestData.ColorIdAsync(server.Services, "Black");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/clothes/colors/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await ClothesTestData.GarmentColorIdsAsync(server.Services, publicGarmentId));
        Assert.Empty(await ClothesTestData.GarmentColorIdsAsync(server.Services, privateGarmentId));

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var garments = await database.Set<ClothesGarment>()
            .Where(garment => garment.Id == publicGarmentId || garment.Id == privateGarmentId)
            .ToArrayAsync();
        Assert.All(garments, garment => Assert.Equal(adminId, garment.UpdatedBy));
        Assert.False(await database.Set<ClothingColor>().AnyAsync(color => color.Id == sourceId));
    }

    [Fact]
    public async Task Category_clearing_is_rejected_and_leaves_references_unchanged()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var garmentId = await ClothesTestData.SeedGarmentAsync(
            server.Services, adminId, name: "Required category", categoryName: "Tops");
        var sourceId = await ClothesTestData.CategoryIdAsync(server.Services, "Tops");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/clothes/categories/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
            csrf);

        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("clothes.category.invalid_replacement", problem!.Code);

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var garment = await database.Set<ClothesGarment>().SingleAsync(value => value.Id == garmentId);
        Assert.Equal(sourceId, garment.CategoryId);
        Assert.True(await database.Set<ClothingCategory>().AnyAsync(category => category.Id == sourceId));
    }

    [Fact]
    public async Task Colour_impact_reports_referenced_state_without_disclosing_private_garments()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync("private-colour-impact", "MemberPass123!");
        await ClothesTestData.SeedGarmentAsync(
            server.Services,
            memberId,
            name: "Private black garment",
            colorNames: ["Black"],
            visibility: RecordVisibility.Private);
        var sourceId = await ClothesTestData.ColorIdAsync(server.Services, "Black");
        using var client = await server.CreateAuthenticatedClientAsync();

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/clothes/colors/{sourceId}/deletion-impact",
            CancellationToken.None);

        Assert.True(impact!.IsReferenced);
        Assert.False(impact.CanDeleteDirectly);
        Assert.True(impact.CanClearReferences);
    }

    private sealed record ProblemPayload(string? Code);
}

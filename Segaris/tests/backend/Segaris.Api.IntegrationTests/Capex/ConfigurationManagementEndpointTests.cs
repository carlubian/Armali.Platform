using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Capex;

public sealed class ConfigurationManagementEndpointTests
{
    [Theory]
    [InlineData("/api/configuration/suppliers/1/deletion-impact")]
    [InlineData("/api/configuration/cost-centers/1/deletion-impact")]
    [InlineData("/api/configuration/currencies/1/deletion-impact")]
    [InlineData("/api/capex/categories/1/deletion-impact")]
    public async Task Management_routes_reject_normal_users(string route)
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");

        using var response = await client.GetAsync(route, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Supplier_crud_trims_appends_moves_and_deletes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(client, "/api/configuration/suppliers", new CatalogItemRequest("  Local shop  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<SupplierResponse>(CancellationToken.None);
        Assert.Equal("Local shop", created!.Name);

        var beforeMove = await client.GetFromJsonAsync<SupplierResponse[]>("/api/configuration/suppliers", CancellationToken.None);
        Assert.Equal(created.Id, beforeMove![^1].Id);

        using var moved = await CapexApi.PostJsonAsync(client, $"/api/configuration/suppliers/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        var afterMove = await client.GetFromJsonAsync<SupplierResponse[]>("/api/configuration/suppliers", CancellationToken.None);
        Assert.Equal(created.Id, afterMove![^2].Id);

        using var updatedResponse = await CapexApi.PutJsonAsync(client, $"/api/configuration/suppliers/{created.Id}", new CatalogItemRequest("Neighbourhood shop"), csrf);
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<SupplierResponse>(CancellationToken.None);
        Assert.Equal("Neighbourhood shop", updated!.Name);

        using var impactResponse = await client.GetAsync($"/api/configuration/suppliers/{created.Id}/deletion-impact", CancellationToken.None);
        var impact = await impactResponse.Content.ReadFromJsonAsync<CatalogDeletionImpactResponse>(CancellationToken.None);
        Assert.False(impact!.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/configuration/suppliers/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Duplicate_names_and_invalid_currency_codes_return_stable_problems()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(client, "/api/configuration/suppliers", new CatalogItemRequest(" amazon "), csrf);
        var duplicateProblem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("configuration.catalog.duplicate_name", duplicateProblem!.Code);

        using var invalidCode = await CapexApi.PostJsonAsync(client, "/api/configuration/currencies", new CurrencyItemRequest("Canadian Dollar", "C4D"), csrf);
        var codeProblem = await invalidCode.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCode.StatusCode);
        Assert.Equal("configuration.currency.invalid_code", codeProblem!.Code);
    }

    [Fact]
    public async Task Referenced_values_report_private_neutral_impact_and_reject_direct_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var suppliers = await client.GetFromJsonAsync<SupplierResponse[]>("/api/configuration/suppliers", CancellationToken.None);
        var supplier = suppliers![0];
        await CapexEntryMutationTests.CreateEntryAsync(server, client, csrf, builder => builder.WithSupplier(supplier.Id).WithVisibility("Private"));

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>($"/api/configuration/suppliers/{supplier.Id}/deletion-impact", CancellationToken.None);
        Assert.True(impact!.IsReferenced);
        Assert.False(impact.CanDeleteDirectly);
        Assert.True(impact.CanClearReferences);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/configuration/suppliers/{supplier.Id}", csrf);
        var problem = await deleted.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
        Assert.Equal("configuration.catalog.referenced", problem!.Code);
    }

    [Fact]
    public async Task Capex_categories_support_create_and_direct_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(client, "/api/capex/categories", new CatalogItemRequest("Subscriptions"), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CapexCategoryResponse>(CancellationToken.None);
        Assert.Equal("Subscriptions", created!.Name);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/capex/categories/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Required_catalogs_reject_deleting_the_last_value()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var currencies = await client.GetFromJsonAsync<CurrencyResponse[]>("/api/configuration/currencies", CancellationToken.None);
        foreach (var currency in currencies![..^1])
        {
            using var deleted = await CapexApi.DeleteAsync(client, $"/api/configuration/currencies/{currency.Id}", csrf);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        using var finalDelete = await CapexApi.DeleteAsync(client, $"/api/configuration/currencies/{currencies[^1].Id}", csrf);
        var problem = await finalDelete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Conflict, finalDelete.StatusCode);
        Assert.Equal("configuration.catalog.required_not_empty", problem!.Code);
    }

    [Fact]
    public async Task Supplier_replacement_migrates_public_and_private_entries_and_audits_the_admin()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-owner", "MemberPass123!");
        var publicEntryId = await CapexTestData.SeedEntryAsync(server.Services, adminId, title: "Public supplier", supplierName: "Amazon");
        var privateEntryId = await CapexTestData.SeedEntryAsync(server.Services, memberId, title: "Private supplier", supplierName: "Amazon", visibility: RecordVisibility.Private);
        var sourceId = await CapexTestData.SupplierIdAsync(server.Services, "Amazon");
        var replacementId = await CapexTestData.SupplierIdAsync(server.Services, "IKEA");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/suppliers/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var entries = await database.Set<CapexEntry>()
            .Where(entry => entry.Id == publicEntryId || entry.Id == privateEntryId)
            .OrderBy(entry => entry.Id)
            .ToArrayAsync();
        Assert.All(entries, entry =>
        {
            Assert.Equal(replacementId, entry.SupplierId);
            Assert.Equal(adminId, entry.UpdatedBy);
            Assert.Equal(TimeSpan.Zero, entry.UpdatedAt.Offset);
        });
        Assert.False(await database.Set<SegarisSupplier>().AnyAsync(value => value.Id == sourceId));
    }

    [Fact]
    public async Task Cost_center_references_can_be_cleared_without_disclosing_entries()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-cost-owner", "MemberPass123!");
        var publicEntryId = await CapexTestData.SeedEntryAsync(server.Services, adminId, title: "Public cost", costCenterName: "Household");
        var privateEntryId = await CapexTestData.SeedEntryAsync(server.Services, memberId, title: "Private cost", costCenterName: "Household", visibility: RecordVisibility.Private);
        var sourceId = await CapexTestData.CostCenterIdAsync(server.Services, "Household");
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
        var entries = await database.Set<CapexEntry>()
            .Where(entry => entry.Id == publicEntryId || entry.Id == privateEntryId)
            .ToArrayAsync();
        Assert.All(entries, entry =>
        {
            Assert.Null(entry.CostCenterId);
            Assert.Equal(adminId, entry.UpdatedBy);
        });
    }

    [Fact]
    public async Task Category_replacement_migrates_references_and_deletes_the_source()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var entryId = await CapexTestData.SeedEntryAsync(server.Services, adminId, title: "Other purchase", categoryName: "Other");
        var sourceId = await CapexTestData.CategoryIdAsync(server.Services, "Other");
        var replacementId = await CapexTestData.CategoryIdAsync(server.Services, "Home");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/capex/categories/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var entry = await database.Set<CapexEntry>().SingleAsync(value => value.Id == entryId);
        Assert.Equal(replacementId, entry.CategoryId);
        Assert.Equal(adminId, entry.UpdatedBy);
        Assert.False(await database.Set<CapexCategory>().AnyAsync(value => value.Id == sourceId));
    }

    private sealed record ProblemPayload(string? Code);
}

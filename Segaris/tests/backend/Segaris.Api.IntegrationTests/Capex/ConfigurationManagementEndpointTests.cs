using System.Net;
using System.Net.Http.Json;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Configuration.Contracts;

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

    private sealed record ProblemPayload(string? Code);
}

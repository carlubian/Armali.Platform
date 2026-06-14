using System.Net;
using System.Net.Http.Json;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Configuration;

namespace Segaris.Api.IntegrationTests.Capex;

public sealed class CapexEntryMutationTests
{
    [Fact]
    public async Task Create_persists_the_entry_with_server_calculated_totals()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var request = (await DefaultBuilderAsync(server))
            .WithTitle("Office refit")
            .WithItems(new("Desks", 2m, 150.00m), new("Chairs", 3m, 49.99m))
            .BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/capex/entries", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CapexEntryResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Office refit", created.Title);
        // 2 * 150.00 + 3 * 49.99 = 300.00 + 149.97 = 449.97, summed from rounded lines.
        Assert.Equal(new[] { 300.00m, 149.97m }, created.Items.Select(item => item.LineAmount).ToArray());
        Assert.Equal(449.97m, created.TotalAmount);
        Assert.Equal(new[] { 0, 1 }, created.Items.Select(item => item.Position).ToArray());

        // The persisted entry is retrievable with the same calculated total.
        var fetched = await client.GetFromJsonAsync<CapexEntryResponse>(
            $"/api/capex/entries/{created.Id}",
            CancellationToken.None);
        Assert.Equal(449.97m, fetched!.TotalAmount);
        Assert.Empty(fetched.Attachments);
    }

    [Fact]
    public async Task Create_accepts_a_zero_total_entry()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var request = (await DefaultBuilderAsync(server))
            .WithItems(new CapexItemRequest("Donated item", 1m, 0m))
            .BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/capex/entries", request, csrf);
        var created = await response.Content.ReadFromJsonAsync<CapexEntryResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(0m, created!.TotalAmount);
    }

    [Fact]
    public async Task Create_without_an_antiforgery_token_is_rejected()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var request = (await DefaultBuilderAsync(server)).BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/capex/entries", request, csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_catalog_reference()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var currencyId = await CapexTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.Default);
        var request = CapexEntryRequestBuilder.Default()
            .WithCategory(999_999)
            .WithCurrency(currencyId)
            .BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/capex/entries", request, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("capex.catalog.unknown_reference", problem!.Code);
    }

    [Fact]
    public async Task Create_rejects_an_invalid_payload()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var request = (await DefaultBuilderAsync(server))
            .WithTitle("   ")
            .BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/capex/entries", request, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("capex.entry.validation", problem!.Code);
    }

    [Fact]
    public async Task Update_replaces_items_changes_fields_and_recomputes_the_total()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var entryId = await CreateEntryAsync(
            server,
            client,
            csrf,
            builder => builder
                .WithTitle("Original")
                .WithStatus("Planning")
                .WithMovementType("Expense")
                .WithItems(new CapexItemRequest("Old line", 1m, 10m)));

        var update = (await DefaultBuilderAsync(server))
            .WithTitle("Revised")
            .WithStatus("Completed")
            .WithMovementType("Income")
            .WithItems(new("Second", 1m, 5m), new("First", 2m, 10m))
            .BuildUpdate();

        using var response = await CapexApi.PutJsonAsync(client, $"/api/capex/entries/{entryId}", update, csrf);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CapexEntryResponse>(CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Revised", updated.Title);
        Assert.Equal("Completed", updated.Status);
        Assert.Equal("Income", updated.MovementType);
        // The submitted order is the persisted position; the total is recomputed.
        Assert.Equal(new[] { "Second", "First" }, updated.Items.Select(item => item.Description).ToArray());
        Assert.Equal(new[] { 0, 1 }, updated.Items.Select(item => item.Position).ToArray());
        Assert.Equal(25.00m, updated.TotalAmount);
    }

    [Fact]
    public async Task Update_of_a_missing_entry_returns_a_capex_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var update = (await DefaultBuilderAsync(server)).BuildUpdate();

        using var response = await CapexApi.PutJsonAsync(client, "/api/capex/entries/9999", update, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("capex.entry.not_found", problem!.Code);
    }

    [Fact]
    public async Task Delete_removes_the_entry_and_makes_it_unreachable()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var entryId = await CreateEntryAsync(server, client, csrf, builder => builder.WithTitle("Disposable"));

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/capex/entries/{entryId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var fetch = await client.GetAsync($"/api/capex/entries/{entryId}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_of_a_missing_entry_returns_a_capex_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.DeleteAsync(client, "/api/capex/entries/9999", csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("capex.entry.not_found", problem!.Code);
    }

    internal static async Task<CapexEntryRequestBuilder> DefaultBuilderAsync(CapexTestServer server)
    {
        var categoryId = await CapexTestData.CategoryIdAsync(server.Services, CapexCategoryCatalog.Codes.Other);
        var currencyId = await CapexTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.Default);
        return CapexEntryRequestBuilder.Default()
            .WithCategory(categoryId)
            .WithCurrency(currencyId);
    }

    internal static async Task<int> CreateEntryAsync(
        CapexTestServer server,
        HttpClient client,
        string csrf,
        Func<CapexEntryRequestBuilder, CapexEntryRequestBuilder> configure)
    {
        var builder = configure(await DefaultBuilderAsync(server));
        using var response = await CapexApi.PostJsonAsync(client, "/api/capex/entries", builder.BuildCreate(), csrf);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CapexEntryResponse>(CancellationToken.None);
        return created!.Id;
    }

    private sealed record ProblemPayload(string? Code);
}

using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Opex;

public sealed class OpexContractMutationTests
{
    [Fact]
    public async Task Create_persists_the_contract_with_creation_defaults()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var request = (await DefaultBuilderAsync(server))
            .WithName("Mobile plan")
            .WithEstimatedAnnualAmount(240m)
            .BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/opex/contracts", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<OpexContractResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Mobile plan", created.Name);
        Assert.Equal("Expense", created.MovementType);
        Assert.Equal("Active", created.Status);
        Assert.Equal("Monthly", created.ExpectedFrequency);
        Assert.Equal(240m, created.EstimatedAnnualAmount);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal(CapexTestServer.AdminUserName, created.CreatedByName);
        Assert.Empty(created.Attachments);

        // The persisted contract is retrievable with the same projection.
        var fetched = await client.GetFromJsonAsync<OpexContractResponse>(
            $"/api/opex/contracts/{created.Id}",
            CancellationToken.None);
        Assert.Equal("Mobile plan", fetched!.Name);
    }

    [Fact]
    public async Task Create_trims_the_name_before_persisting()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var request = (await DefaultBuilderAsync(server)).WithName("  Spaced name  ").BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/opex/contracts", request, csrf);
        var created = await response.Content.ReadFromJsonAsync<OpexContractResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Spaced name", created!.Name);
    }

    [Fact]
    public async Task Create_without_an_antiforgery_token_is_rejected()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var request = (await DefaultBuilderAsync(server)).BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/opex/contracts", request, csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_an_invalid_payload()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var request = (await DefaultBuilderAsync(server)).WithName("   ").BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/opex/contracts", request, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("opex.contract.validation", problem!.Code);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_catalog_reference()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var currencyId = await OpexTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.Default);
        var request = OpexContractRequestBuilder.Default()
            .WithCategory(999_999)
            .WithCurrency(currencyId)
            .BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/opex/contracts", request, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("opex.catalog.unknown_reference", problem!.Code);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_name_ignoring_case_and_whitespace()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        await CreateContractAsync(server, client, csrf, builder => builder.WithName("Internet line"));

        var duplicate = (await DefaultBuilderAsync(server)).WithName("  internet LINE  ").BuildCreate();
        using var response = await CapexApi.PostJsonAsync(client, "/api/opex/contracts", duplicate, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("opex.contract.duplicate_name", problem!.Code);
    }

    [Fact]
    public async Task Update_replaces_every_editable_field()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await CreateContractAsync(
            server,
            client,
            csrf,
            builder => builder
                .WithName("Original")
                .WithStatus("Planning")
                .WithMovementType("Expense")
                .WithExpectedFrequency("Monthly"));

        var update = (await DefaultBuilderAsync(server))
            .WithName("Revised")
            .WithStatus("Closed")
            .WithMovementType("Income")
            .WithExpectedFrequency("Annual")
            .WithEstimatedAnnualAmount(1200m)
            .WithNotes("Updated terms")
            .BuildUpdate();

        using var response = await CapexApi.PutJsonAsync(client, $"/api/opex/contracts/{contractId}", update, csrf);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<OpexContractResponse>(CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Revised", updated.Name);
        Assert.Equal("Closed", updated.Status);
        Assert.Equal("Income", updated.MovementType);
        Assert.Equal("Annual", updated.ExpectedFrequency);
        Assert.Equal(1200m, updated.EstimatedAnnualAmount);
        Assert.Equal("Updated terms", updated.Notes);
    }

    [Fact]
    public async Task Update_to_its_own_name_is_not_a_duplicate_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await CreateContractAsync(server, client, csrf, builder => builder.WithName("Stable name"));

        var update = (await DefaultBuilderAsync(server)).WithName("Stable name").WithStatus("OnHold").BuildUpdate();
        using var response = await CapexApi.PutJsonAsync(client, $"/api/opex/contracts/{contractId}", update, csrf);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_to_an_existing_name_returns_a_duplicate_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        await CreateContractAsync(server, client, csrf, builder => builder.WithName("Taken"));
        var contractId = await CreateContractAsync(server, client, csrf, builder => builder.WithName("Free"));

        var update = (await DefaultBuilderAsync(server)).WithName("taken").BuildUpdate();
        using var response = await CapexApi.PutJsonAsync(client, $"/api/opex/contracts/{contractId}", update, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("opex.contract.duplicate_name", problem!.Code);
    }

    [Fact]
    public async Task Update_of_a_missing_contract_returns_an_opex_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var update = (await DefaultBuilderAsync(server)).BuildUpdate();

        using var response = await CapexApi.PutJsonAsync(client, "/api/opex/contracts/999999", update, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.contract.not_found", problem!.Code);
    }

    [Fact]
    public async Task Any_user_may_edit_a_public_contract_created_by_another()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var contractId = await CreateContractAsync(
            server, founder, founderCsrf, builder => builder.WithName("Shared").WithVisibility("Public"));

        await server.CreateUserAsync("collaborator", "CollaboratorPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "collaborator", "CollaboratorPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        var update = (await DefaultBuilderAsync(server)).WithName("Shared").WithStatus("OnHold").BuildUpdate();
        using var response = await CapexApi.PutJsonAsync(member, $"/api/opex/contracts/{contractId}", update, memberCsrf);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Only_the_creator_may_change_visibility()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var contractId = await CreateContractAsync(
            server, founder, founderCsrf, builder => builder.WithName("Owned").WithVisibility("Public"));

        await server.CreateUserAsync("intruder", "IntruderPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "intruder", "IntruderPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        var update = (await DefaultBuilderAsync(server)).WithName("Owned").WithVisibility("Private").BuildUpdate();
        using var response = await CapexApi.PutJsonAsync(member, $"/api/opex/contracts/{contractId}", update, memberCsrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("opex.contract.visibility_forbidden", problem!.Code);
    }

    [Fact]
    public async Task Another_users_private_contract_cannot_be_updated_and_is_indistinguishable_from_missing()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        var memberId = await server.CreateUserAsync("owner", "OwnerPass123!");
        var privateId = await OpexTestData.SeedContractAsync(
            server.Services, memberId, name: "Owner private", visibility: RecordVisibility.Private);

        var update = (await DefaultBuilderAsync(server)).WithName("Hijacked").BuildUpdate();
        using var response = await CapexApi.PutJsonAsync(admin, $"/api/opex/contracts/{privateId}", update, adminCsrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.contract.not_found", problem!.Code);
    }

    [Fact]
    public async Task Delete_removes_the_contract_and_makes_it_unreachable()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await CreateContractAsync(server, client, csrf, builder => builder.WithName("Disposable"));

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/opex/contracts/{contractId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var fetch = await client.GetAsync($"/api/opex/contracts/{contractId}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_cascades_to_occurrences()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var contractId = await OpexTestData.SeedContractAsync(
            server.Services,
            founderId,
            name: "With movements",
            occurrences: [(new DateOnly(2026, 1, 10), 50m), (new DateOnly(2026, 2, 10), 75m)]);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/opex/contracts/{contractId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        Assert.Equal(0, await OpexTestData.OccurrenceCountAsync(server.Services, contractId));
    }

    [Fact]
    public async Task Delete_of_a_missing_contract_returns_an_opex_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.DeleteAsync(client, "/api/opex/contracts/999999", csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.contract.not_found", problem!.Code);
    }

    internal static async Task<OpexContractRequestBuilder> DefaultBuilderAsync(CapexTestServer server)
    {
        var categoryId = await OpexTestData.CategoryIdAsync(server.Services, "Other");
        var currencyId = await OpexTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.Default);
        return OpexContractRequestBuilder.Default()
            .WithCategory(categoryId)
            .WithCurrency(currencyId);
    }

    internal static async Task<int> CreateContractAsync(
        CapexTestServer server,
        HttpClient client,
        string csrf,
        Func<OpexContractRequestBuilder, OpexContractRequestBuilder> configure)
    {
        var builder = configure(await DefaultBuilderAsync(server));
        using var response = await CapexApi.PostJsonAsync(client, "/api/opex/contracts", builder.BuildCreate(), csrf);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<OpexContractResponse>(CancellationToken.None);
        return created!.Id;
    }

    private sealed record ProblemPayload(string? Code);
}

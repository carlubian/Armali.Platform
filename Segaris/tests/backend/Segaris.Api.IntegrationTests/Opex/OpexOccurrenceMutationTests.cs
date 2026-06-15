using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Shared.Api;

namespace Segaris.Api.IntegrationTests.Opex;

public sealed class OpexOccurrenceMutationTests
{
    [Fact]
    public async Task Create_persists_the_occurrence_within_its_contract()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        var request = OpexOccurrenceRequestBuilder.Default()
            .WithEffectiveDate(new DateOnly(2026, 3, 10))
            .WithActualAmount(125.50m)
            .WithDescription("March invoice")
            .BuildCreate();

        using var response = await CapexApi.PostJsonAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<OpexOccurrenceResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal(contractId, created.ContractId);
        Assert.Equal(new DateOnly(2026, 3, 10), created.EffectiveDate);
        Assert.Equal(125.50m, created.ActualAmount);
        Assert.Equal("March invoice", created.Description);
        Assert.Equal(CapexTestServer.AdminUserName, created.CreatedByName);
        Assert.Empty(created.Attachments);

        // The persisted occurrence is retrievable with the same projection.
        var fetched = await client.GetFromJsonAsync<OpexOccurrenceResponse>(
            $"/api/opex/contracts/{contractId}/occurrences/{created.Id}",
            CancellationToken.None);
        Assert.Equal("March invoice", fetched!.Description);
    }

    [Fact]
    public async Task Create_trims_the_description_before_persisting()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        var request = OpexOccurrenceRequestBuilder.Default().WithDescription("  Spaced  ").BuildCreate();
        using var response = await CapexApi.PostJsonAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences", request, csrf);
        var created = await response.Content.ReadFromJsonAsync<OpexOccurrenceResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Spaced", created!.Description);
    }

    [Fact]
    public async Task Create_accepts_a_zero_amount_and_a_past_or_future_date()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        var past = OpexOccurrenceRequestBuilder.Default()
            .WithEffectiveDate(new DateOnly(1999, 1, 1)).WithActualAmount(0m).BuildCreate();
        var future = OpexOccurrenceRequestBuilder.Default()
            .WithEffectiveDate(new DateOnly(2099, 12, 31)).WithActualAmount(0m).BuildCreate();

        using var pastResponse = await CapexApi.PostJsonAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences", past, csrf);
        using var futureResponse = await CapexApi.PostJsonAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences", future, csrf);

        Assert.Equal(HttpStatusCode.Created, pastResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, futureResponse.StatusCode);
    }

    [Fact]
    public async Task Create_without_an_antiforgery_token_is_rejected()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/opex/contracts/{contractId}/occurrences",
            OpexOccurrenceRequestBuilder.Default().BuildCreate(),
            csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_requires_an_effective_date()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        var request = OpexOccurrenceRequestBuilder.Default().WithEffectiveDate(null).BuildCreate();
        using var response = await CapexApi.PostJsonAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences", request, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("opex.occurrence.validation", problem!.Code);
    }

    [Fact]
    public async Task Create_rejects_a_negative_amount()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        var request = OpexOccurrenceRequestBuilder.Default().WithActualAmount(-1m).BuildCreate();
        using var response = await CapexApi.PostJsonAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences", request, csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("opex.occurrence.validation", problem!.Code);
    }

    [Fact]
    public async Task Create_under_a_missing_contract_returns_a_contract_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/opex/contracts/999999/occurrences",
            OpexOccurrenceRequestBuilder.Default().BuildCreate(),
            csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.contract.not_found", problem!.Code);
    }

    [Fact]
    public async Task Update_replaces_every_editable_field()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);
        var occurrenceId = await CreateOccurrenceAsync(client, csrf, contractId, builder => builder);

        var update = OpexOccurrenceRequestBuilder.Default()
            .WithEffectiveDate(new DateOnly(2026, 7, 1))
            .WithActualAmount(999.99m)
            .WithDescription("Revised")
            .WithNotes("Adjusted")
            .BuildUpdate();

        using var response = await CapexApi.PutJsonAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}", update, csrf);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<OpexOccurrenceResponse>(CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(new DateOnly(2026, 7, 1), updated.EffectiveDate);
        Assert.Equal(999.99m, updated.ActualAmount);
        Assert.Equal("Revised", updated.Description);
        Assert.Equal("Adjusted", updated.Notes);
    }

    [Fact]
    public async Task Update_of_a_missing_occurrence_returns_an_occurrence_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        using var response = await CapexApi.PutJsonAsync(
            client,
            $"/api/opex/contracts/{contractId}/occurrences/424242",
            OpexOccurrenceRequestBuilder.Default().BuildUpdate(),
            csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.occurrence.not_found", problem!.Code);
    }

    [Fact]
    public async Task Delete_removes_the_occurrence_and_makes_it_unreachable()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);
        var occurrenceId = await CreateOccurrenceAsync(client, csrf, contractId, builder => builder);

        using var deleted = await CapexApi.DeleteAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var fetch = await client.GetAsync(
            $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_of_a_missing_occurrence_returns_an_occurrence_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        using var response = await CapexApi.DeleteAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences/424242", csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.occurrence.not_found", problem!.Code);
    }

    [Fact]
    public async Task Occurrence_mutations_refresh_the_contract_realized_current_year_total()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);
        var thisYear = DateTime.UtcNow.Year;

        var occurrenceId = await CreateOccurrenceAsync(
            client, csrf, contractId,
            builder => builder.WithEffectiveDate(new DateOnly(thisYear, 1, 15)).WithActualAmount(100m));
        Assert.Equal(100m, await RealizedTotalAsync(client, contractId));

        // Adding a second movement and editing the first is immediately reflected.
        await CreateOccurrenceAsync(
            client, csrf, contractId,
            builder => builder.WithEffectiveDate(new DateOnly(thisYear, 2, 15)).WithActualAmount(50m));
        Assert.Equal(150m, await RealizedTotalAsync(client, contractId));

        var update = OpexOccurrenceRequestBuilder.Default()
            .WithEffectiveDate(new DateOnly(thisYear, 1, 15)).WithActualAmount(200m).BuildUpdate();
        using var updateResponse = await CapexApi.PutJsonAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}", update, csrf);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(250m, await RealizedTotalAsync(client, contractId));

        // Deleting the second movement is reflected as well.
        using var delete = await CapexApi.DeleteAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(50m, await RealizedTotalAsync(client, contractId));
    }

    private static async Task<decimal> RealizedTotalAsync(HttpClient client, int contractId)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<OpexContractSummaryResponse>>(
            "/api/opex/contracts",
            CancellationToken.None);
        var summary = page!.Items.Single(item => item.Id == contractId);
        return summary.RealizedCurrentYearAmount;
    }

    internal static async Task<int> CreateOccurrenceAsync(
        HttpClient client,
        string csrf,
        int contractId,
        Func<OpexOccurrenceRequestBuilder, OpexOccurrenceRequestBuilder> configure)
    {
        var builder = configure(OpexOccurrenceRequestBuilder.Default());
        using var response = await CapexApi.PostJsonAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences", builder.BuildCreate(), csrf);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<OpexOccurrenceResponse>(CancellationToken.None);
        return created!.Id;
    }

    private sealed record ProblemPayload(string? Code);
}

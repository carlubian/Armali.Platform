using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Shared.Api;

namespace Segaris.Api.IntegrationTests.Opex;

public sealed class OpexOccurrenceListTests
{
    [Fact]
    public async Task A_contract_without_occurrences_returns_an_empty_page()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        var page = await GetPageAsync(client, $"/api/opex/contracts/{contractId}/occurrences");

        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task Occurrences_are_ordered_by_effective_date_then_identifier()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        // Seeded out of chronological order; two share a date to exercise the
        // identifier tie-breaker. Most recent effective date is listed first,
        // and among ties the most recently created occurrence comes first.
        var march = await OpexOccurrenceMutationTests.CreateOccurrenceAsync(
            client, csrf, contractId, b => b.WithEffectiveDate(new DateOnly(2026, 3, 1)));
        var januaryFirst = await OpexOccurrenceMutationTests.CreateOccurrenceAsync(
            client, csrf, contractId, b => b.WithEffectiveDate(new DateOnly(2026, 1, 10)));
        var januarySecond = await OpexOccurrenceMutationTests.CreateOccurrenceAsync(
            client, csrf, contractId, b => b.WithEffectiveDate(new DateOnly(2026, 1, 10)));

        var page = await GetPageAsync(client, $"/api/opex/contracts/{contractId}/occurrences");

        Assert.Equal(
            new[] { march, januarySecond, januaryFirst },
            page.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task Occurrences_paginate_at_the_database_level_with_a_total_count()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);
        for (var day = 1; day <= 7; day++)
        {
            await OpexOccurrenceMutationTests.CreateOccurrenceAsync(
                client, csrf, contractId, b => b.WithEffectiveDate(new DateOnly(2026, 1, day)));
        }

        var firstPage = await GetPageAsync(client, $"/api/opex/contracts/{contractId}/occurrences?page=1&pageSize=5");
        var secondPage = await GetPageAsync(client, $"/api/opex/contracts/{contractId}/occurrences?page=2&pageSize=5");
        var beyond = await GetPageAsync(client, $"/api/opex/contracts/{contractId}/occurrences?page=9&pageSize=5");

        Assert.Equal(7, firstPage.TotalCount);
        Assert.Equal(5, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.Empty(beyond.Items);
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    public async Task Occurrences_reject_out_of_range_pagination(string queryString, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        using var response = await client.GetAsync(
            $"/api/opex/contracts/{contractId}/occurrences?{queryString}", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Listing_under_a_missing_contract_returns_a_contract_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync("/api/opex/contracts/999999/occurrences", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.contract.not_found", problem!.Code);
    }

    [Fact]
    public async Task Detail_of_a_missing_occurrence_returns_an_occurrence_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);

        using var response = await client.GetAsync(
            $"/api/opex/contracts/{contractId}/occurrences/424242", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("opex.occurrence.not_found", problem!.Code);
    }

    private static async Task<PaginatedResponse<OpexOccurrenceSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<OpexOccurrenceSummaryResponse>>(
            route,
            CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(
        string? Code,
        IReadOnlyDictionary<string, string[]>? Errors);
}

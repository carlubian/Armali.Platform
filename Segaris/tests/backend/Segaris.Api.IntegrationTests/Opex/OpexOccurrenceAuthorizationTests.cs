using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Opex;

public sealed class OpexOccurrenceAuthorizationTests
{
    private const string MemberName = "occurrence-outsider";
    private const string MemberPassword = "OutsiderPass123!";

    [Fact]
    public async Task Occurrences_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/opex/contracts/1/occurrences", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_private_parent_contract_hides_all_occurrence_routes_from_other_users()
    {
        using var server = new CapexTestServer();
        var ownerId = await server.CreateUserAsync("occurrence-owner", "OwnerPass123!");
        var contractId = await OpexTestData.SeedContractAsync(
            server.Services,
            ownerId,
            name: "Owner private",
            visibility: RecordVisibility.Private,
            occurrences: [(new DateOnly(2026, 4, 1), 40m)]);

        await server.CreateUserAsync(MemberName, MemberPassword);
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, MemberName, MemberPassword);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync(
            $"/api/opex/contracts/{contractId}/occurrences", CancellationToken.None);
        // The occurrence identifier is unknown to the outsider; even guessing one is
        // reported as a contract not-found so the private contract is not disclosed.
        using var detail = await member.GetAsync(
            $"/api/opex/contracts/{contractId}/occurrences/1", CancellationToken.None);
        using var create = await CapexApi.PostJsonAsync(
            member,
            $"/api/opex/contracts/{contractId}/occurrences",
            OpexOccurrenceRequestBuilder.Default().BuildCreate(),
            memberCsrf);
        using var update = await CapexApi.PutJsonAsync(
            member,
            $"/api/opex/contracts/{contractId}/occurrences/1",
            OpexOccurrenceRequestBuilder.Default().BuildUpdate(),
            memberCsrf);
        using var delete = await CapexApi.DeleteAsync(
            member, $"/api/opex/contracts/{contractId}/occurrences/1", memberCsrf);

        foreach (var response in new[] { list, detail, create, update, delete })
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
            Assert.Equal("opex.contract.not_found", problem!.Code);
        }
    }

    [Fact]
    public async Task An_occurrence_cannot_be_reached_through_a_different_contract()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var contractA = await OpexContractMutationTests.CreateContractAsync(
            server, client, csrf, builder => builder.WithName("Contract A"));
        var contractB = await OpexContractMutationTests.CreateContractAsync(
            server, client, csrf, builder => builder.WithName("Contract B"));
        var occurrenceInA = await OpexOccurrenceMutationTests.CreateOccurrenceAsync(
            client, csrf, contractA, b => b);

        // Addressing A's occurrence under contract B (also accessible to this user)
        // must not resolve: the occurrence does not belong to that contract.
        using var detail = await client.GetAsync(
            $"/api/opex/contracts/{contractB}/occurrences/{occurrenceInA}", CancellationToken.None);
        using var update = await CapexApi.PutJsonAsync(
            client,
            $"/api/opex/contracts/{contractB}/occurrences/{occurrenceInA}",
            OpexOccurrenceRequestBuilder.Default().BuildUpdate(),
            csrf);
        using var delete = await CapexApi.DeleteAsync(
            client, $"/api/opex/contracts/{contractB}/occurrences/{occurrenceInA}", csrf);

        foreach (var response in new[] { detail, update, delete })
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
            Assert.Equal("opex.occurrence.not_found", problem!.Code);
        }

        // The occurrence is still reachable through its real parent.
        using var realParent = await client.GetAsync(
            $"/api/opex/contracts/{contractA}/occurrences/{occurrenceInA}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, realParent.StatusCode);
    }

    [Fact]
    public async Task Any_user_may_manage_occurrences_of_a_public_contract_created_by_another()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var contractId = await OpexContractMutationTests.CreateContractAsync(
            server, founder, founderCsrf, builder => builder.WithName("Shared").WithVisibility("Public"));

        await server.CreateUserAsync("occurrence-collaborator", "CollabPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "occurrence-collaborator", "CollabPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var create = await CapexApi.PostJsonAsync(
            member,
            $"/api/opex/contracts/{contractId}/occurrences",
            OpexOccurrenceRequestBuilder.Default().BuildCreate(),
            memberCsrf);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    private sealed record ProblemPayload(string? Code);
}

using System.Net;
using System.Net.Http.Json;
using Segaris.Api.Modules.Capex.Contracts;

namespace Segaris.Api.IntegrationTests.Capex;

/// <summary>
/// Two-user coverage for the Capex mutation authorization rules: public entries
/// are collaborative, private entries are isolated to their creator, and only the
/// creator may change an entry's visibility.
/// </summary>
public sealed class CapexEntryAuthorizationTests
{
    private const string MemberName = "collaborator";
    private const string MemberPassword = "CollaboratorPass123!";

    [Fact]
    public async Task Any_user_can_edit_and_delete_a_public_entry()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(
            server,
            founder,
            founderCsrf,
            builder => builder.WithTitle("Shared public").WithVisibility("Public"));

        var member = await CreateMemberClientAsync(server);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        var update = (await CapexEntryMutationTests.DefaultBuilderAsync(server))
            .WithTitle("Edited by collaborator")
            .WithVisibility("Public")
            .WithItems(new CapexItemRequest("Collaborator line", 1m, 7m))
            .BuildUpdate();
        using var edit = await CapexApi.PutJsonAsync(member, $"/api/capex/entries/{entryId}", update, memberCsrf);
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

        using var delete = await CapexApi.DeleteAsync(member, $"/api/capex/entries/{entryId}", memberCsrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task A_private_entry_is_isolated_from_non_creators()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(
            server,
            founder,
            founderCsrf,
            builder => builder.WithTitle("Founder private").WithVisibility("Private"));

        var member = await CreateMemberClientAsync(server);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var update = (await CapexEntryMutationTests.DefaultBuilderAsync(server))
            .WithVisibility("Private")
            .BuildUpdate();

        using var edit = await CapexApi.PutJsonAsync(member, $"/api/capex/entries/{entryId}", update, memberCsrf);
        using var delete = await CapexApi.DeleteAsync(member, $"/api/capex/entries/{entryId}", memberCsrf);

        // The private entry is invisible, so both mutations return the not-found privacy code.
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var problem = await edit.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("capex.entry.not_found", problem!.Code);
    }

    [Fact]
    public async Task A_non_creator_cannot_appropriate_a_public_entrys_visibility()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(
            server,
            founder,
            founderCsrf,
            builder => builder.WithTitle("Founder public").WithVisibility("Public"));

        var member = await CreateMemberClientAsync(server);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var update = (await CapexEntryMutationTests.DefaultBuilderAsync(server))
            .WithTitle("Founder public")
            .WithVisibility("Private")
            .BuildUpdate();

        using var response = await CapexApi.PutJsonAsync(member, $"/api/capex/entries/{entryId}", update, memberCsrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("capex.entry.visibility_forbidden", problem!.Code);
    }

    [Fact]
    public async Task The_creator_can_change_their_entrys_visibility()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(
            server,
            founder,
            founderCsrf,
            builder => builder.WithTitle("Founder owned").WithVisibility("Public"));

        var update = (await CapexEntryMutationTests.DefaultBuilderAsync(server))
            .WithTitle("Founder owned")
            .WithVisibility("Private")
            .BuildUpdate();

        using var response = await CapexApi.PutJsonAsync(founder, $"/api/capex/entries/{entryId}", update, founderCsrf);
        var updated = await response.Content.ReadFromJsonAsync<CapexEntryResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Private", updated!.Visibility);
    }

    private static async Task<HttpClient> CreateMemberClientAsync(CapexTestServer server)
    {
        await server.CreateUserAsync(MemberName, MemberPassword);
        var client = server.CreateClient();
        await CapexTestServer.LoginAsync(client, MemberName, MemberPassword);
        return client;
    }

    private sealed record ProblemPayload(string? Code);
}

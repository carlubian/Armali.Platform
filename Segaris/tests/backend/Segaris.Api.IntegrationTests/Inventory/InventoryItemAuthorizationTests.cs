using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryItemAuthorizationTests
{
    private const string MemberName = "inventory-collaborator";
    private const string MemberPassword = "InventoryCollaborator123!";

    [Fact]
    public async Task Any_user_can_edit_a_public_item_but_private_items_are_isolated()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicItemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Shared", visibility: RecordVisibility.Public);
        var privateItemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Private", visibility: RecordVisibility.Private);

        var member = await CreateMemberClientAsync(server);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        using var editPublic = await CapexApi.PutJsonAsync(
            member,
            $"/api/inventory/items/{publicItemId}",
            (await InventoryItemMutationTests.DefaultBuilderAsync(server))
                .WithName("Shared edited")
                .WithVisibility("Public")
                .BuildUpdate(),
            memberCsrf);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/inventory/items/{privateItemId}",
            (await InventoryItemMutationTests.DefaultBuilderAsync(server))
                .WithName("Private edited")
                .WithVisibility("Private")
                .BuildUpdate(),
            memberCsrf);

        Assert.Equal(HttpStatusCode.OK, editPublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
        var problem = await editPrivate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("inventory.item.not_found", problem!.Code);
    }

    [Fact]
    public async Task A_non_creator_cannot_make_a_public_item_private()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Shared", visibility: RecordVisibility.Public);

        var member = await CreateMemberClientAsync(server);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        using var response = await CapexApi.PutJsonAsync(
            member,
            $"/api/inventory/items/{itemId}",
            (await InventoryItemMutationTests.DefaultBuilderAsync(server))
                .WithName("Shared")
                .WithVisibility("Private")
                .BuildUpdate(),
            memberCsrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("inventory.item.visibility_forbidden", problem!.Code);
    }

    [Fact]
    public async Task Creator_cannot_make_item_private_when_it_appears_in_public_order()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Ordered", visibility: RecordVisibility.Public);
        await InventoryTestData.SeedOrderAsync(server.Services, founderId, itemId, visibility: RecordVisibility.Public);

        using var response = await CapexApi.PutJsonAsync(
            founder,
            $"/api/inventory/items/{itemId}",
            (await InventoryItemMutationTests.DefaultBuilderAsync(server))
                .WithName("Ordered")
                .WithVisibility("Private")
                .BuildUpdate(),
            founderCsrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("inventory.item.visibility_forbidden", problem!.Code);
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

using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryAttentionTests
{
    [Fact]
    public async Task Inventory_reports_no_attention_without_qualifying_items()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        Assert.False(await InventoryAttentionAsync(client));
    }

    [Theory]
    [InlineData("Active", 1, 5, true)]
    [InlineData("Active", 5, 5, true)]
    [InlineData("Active", 0, 0, true)]
    [InlineData("Active", 6, 5, false)]
    [InlineData("Candidate", 1, 5, false)]
    [InlineData("Deprecated", 1, 5, false)]
    public async Task Attention_activates_only_for_active_low_or_equal_stock_items(
        string status,
        decimal currentStock,
        decimal minimumStock,
        bool expected)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Tracked",
            status: Enum.Parse<InventoryItemStatus>(status),
            currentStock: currentStock,
            minimumStock: minimumStock);

        Assert.Equal(expected, await InventoryAttentionAsync(client));
    }

    [Fact]
    public async Task Another_users_private_low_stock_item_does_not_activate_attention_for_others()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        await InventoryTestData.SeedItemAsync(
            server.Services,
            memberId,
            name: "Private low",
            status: InventoryItemStatus.Active,
            currentStock: 0m,
            minimumStock: 3m,
            visibility: RecordVisibility.Private);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        // No privacy bypass for the administrator; the owning member is alerted.
        Assert.False(await InventoryAttentionAsync(admin));
        Assert.True(await InventoryAttentionAsync(member));
    }

    [Fact]
    public async Task Public_low_stock_item_activates_attention_for_every_user()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        await InventoryTestData.SeedItemAsync(
            server.Services,
            memberId,
            name: "Public low",
            status: InventoryItemStatus.Active,
            currentStock: 0m,
            minimumStock: 3m);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        Assert.True(await InventoryAttentionAsync(admin));
        Assert.True(await InventoryAttentionAsync(member));
    }

    private static async Task<bool> InventoryAttentionAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<LauncherAttentionResponse>(
            "/api/launcher/attention",
            CancellationToken.None);
        Assert.NotNull(response);
        var inventory = Assert.Single(response.Modules, module => module.Module == InventoryLauncherCard.ModuleKey);
        return inventory.RequiresAttention;
    }
}

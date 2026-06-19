using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Assets;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Shared.Authorization;
using Segaris.Shared.Time;

namespace Segaris.Api.IntegrationTests.Assets;

public sealed class AssetsAttentionTests
{
    [Fact]
    public async Task Assets_reports_no_attention_without_qualifying_assets()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        Assert.False(await AssetsAttentionAsync(client));
    }

    [Theory]
    [InlineData(0, "Active", true)]
    [InlineData(30, "Active", true)]
    [InlineData(-1, "Active", false)]
    [InlineData(31, "Active", false)]
    [InlineData(15, "Stored", true)]
    [InlineData(15, "Retired", false)]
    public async Task Attention_matches_expected_end_of_life_window_and_status(
        int endOfLifeOffsetDays,
        string status,
        bool expected)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Tracked",
            status: Enum.Parse<AssetStatus>(status),
            expectedEndOfLifeDate: Today(server.Services).AddDays(endOfLifeOffsetDays));

        Assert.Equal(expected, await AssetsAttentionAsync(client));
    }

    [Fact]
    public async Task Asset_without_expected_end_of_life_does_not_activate_attention()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Undated",
            expectedEndOfLifeDate: null);

        Assert.False(await AssetsAttentionAsync(client));
    }

    [Fact]
    public async Task Another_users_private_qualifying_asset_does_not_activate_attention_for_others()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        await AssetsTestData.SeedAssetAsync(
            server.Services,
            memberId,
            name: "Private expiring",
            expectedEndOfLifeDate: Today(server.Services).AddDays(10),
            visibility: RecordVisibility.Private);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        Assert.False(await AssetsAttentionAsync(admin));
        Assert.True(await AssetsAttentionAsync(member));
    }

    [Fact]
    public async Task Public_qualifying_asset_activates_attention_for_every_user()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        await AssetsTestData.SeedAssetAsync(
            server.Services,
            memberId,
            name: "Public expiring",
            expectedEndOfLifeDate: Today(server.Services).AddDays(10));

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        Assert.True(await AssetsAttentionAsync(admin));
        Assert.True(await AssetsAttentionAsync(member));
    }

    private static DateOnly Today(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        return AssetsCivilDate.Today(clock);
    }

    private static async Task<bool> AssetsAttentionAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<LauncherAttentionResponse>(
            "/api/launcher/attention",
            CancellationToken.None);
        Assert.NotNull(response);
        var assets = Assert.Single(response.Modules, module => module.Module == AssetsLauncherCard.ModuleKey);
        return assets.RequiresAttention;
    }
}

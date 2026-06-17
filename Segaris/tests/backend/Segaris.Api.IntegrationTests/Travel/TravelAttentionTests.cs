using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Travel;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Shared.Authorization;
using Segaris.Shared.Time;

namespace Segaris.Api.IntegrationTests.Travel;

public sealed class TravelAttentionTests
{
    [Fact]
    public async Task Travel_reports_no_attention_without_qualifying_trips()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        Assert.False(await TravelAttentionAsync(client));
    }

    [Theory]
    [InlineData("Ongoing", -30, true)]
    [InlineData("Planned", 0, true)]
    [InlineData("Planned", 7, true)]
    [InlineData("Planned", 8, false)]
    [InlineData("Planned", -1, false)]
    [InlineData("Completed", 1, false)]
    [InlineData("Cancelled", 1, false)]
    public async Task Attention_matches_status_and_madrid_seven_day_window(
        string status,
        int startOffsetDays,
        bool expected)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var today = Today(server.Services);
        var start = today.AddDays(startOffsetDays);
        await TravelTestData.SeedTripAsync(
            server.Services,
            founderId,
            name: "Tracked",
            startDate: start,
            endDate: start.AddDays(1),
            status: Enum.Parse<TravelTripStatus>(status));

        Assert.Equal(expected, await TravelAttentionAsync(client));
    }

    [Fact]
    public async Task Madrid_date_boundary_uses_household_timezone()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var today = Today(server.Services);
        await TravelTestData.SeedTripAsync(
            server.Services,
            founderId,
            name: "Boundary",
            startDate: today.AddDays(7),
            endDate: today.AddDays(8),
            status: TravelTripStatus.Planned);

        Assert.True(await TravelAttentionAsync(client));
    }

    [Fact]
    public async Task Another_users_private_qualifying_trip_does_not_activate_attention_for_others()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        var today = Today(server.Services);
        await TravelTestData.SeedTripAsync(
            server.Services,
            memberId,
            name: "Private soon",
            startDate: today.AddDays(1),
            endDate: today.AddDays(2),
            visibility: RecordVisibility.Private);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        Assert.False(await TravelAttentionAsync(admin));
        Assert.True(await TravelAttentionAsync(member));
    }

    [Fact]
    public async Task Public_qualifying_trip_activates_attention_for_every_user()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        var today = Today(server.Services);
        await TravelTestData.SeedTripAsync(
            server.Services,
            memberId,
            name: "Public soon",
            startDate: today.AddDays(1),
            endDate: today.AddDays(2));

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        Assert.True(await TravelAttentionAsync(admin));
        Assert.True(await TravelAttentionAsync(member));
    }

    private static DateOnly Today(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        return TravelDefaults.Today(clock.UtcNow);
    }

    private static async Task<bool> TravelAttentionAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<LauncherAttentionResponse>(
            "/api/launcher/attention",
            CancellationToken.None);
        Assert.NotNull(response);
        var travel = Assert.Single(response.Modules, module => module.Module == TravelLauncherCard.ModuleKey);
        return travel.RequiresAttention;
    }
}

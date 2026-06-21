using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Firebird;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Shared.Authorization;
using Segaris.Shared.Time;

namespace Segaris.Api.IntegrationTests.Firebird;

public sealed class FirebirdAttentionTests
{
    [Fact]
    public async Task Firebird_reports_no_attention_without_qualifying_people()
    {
        using var server = CreateServer(new DateOnly(2026, 6, 21));
        using var client = await server.CreateAuthenticatedClientAsync();

        Assert.False(await FirebirdAttentionAsync(client));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(-1, false)]
    [InlineData(8, false)]
    public async Task Attention_matches_inclusive_seven_day_birthday_window(int birthdayOffsetDays, bool expected)
    {
        var today = new DateOnly(2026, 6, 21);
        using var server = CreateServer(today);
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var birthday = today.AddDays(birthdayOffsetDays);
        await FirebirdTestData.SeedPersonAsync(
            server.Services,
            founderId,
            name: "Tracked",
            birthdayMonth: birthday.Month,
            birthdayDay: birthday.Day);

        Assert.Equal(expected, await FirebirdAttentionAsync(client));
    }

    [Fact]
    public async Task Attention_wraps_across_the_year_boundary()
    {
        using var server = CreateServer(new DateOnly(2026, 12, 28));
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await FirebirdTestData.SeedPersonAsync(
            server.Services,
            founderId,
            name: "New year birthday",
            birthdayMonth: 1,
            birthdayDay: 3);

        Assert.True(await FirebirdAttentionAsync(client));
    }

    [Fact]
    public async Task Leap_day_birthday_is_observed_on_march_first_in_non_leap_years()
    {
        using var server = CreateServer(new DateOnly(2026, 2, 23));
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await FirebirdTestData.SeedPersonAsync(
            server.Services,
            founderId,
            name: "Leap day birthday",
            birthdayMonth: 2,
            birthdayDay: 29);

        Assert.True(await FirebirdAttentionAsync(client));
    }

    [Fact]
    public async Task Person_without_birthday_does_not_activate_attention()
    {
        using var server = CreateServer(new DateOnly(2026, 6, 21));
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await FirebirdTestData.SeedPersonAsync(server.Services, founderId, name: "Undated");

        Assert.False(await FirebirdAttentionAsync(client));
    }

    [Fact]
    public async Task Another_users_private_qualifying_person_does_not_activate_attention_for_others()
    {
        var today = new DateOnly(2026, 6, 21);
        using var server = CreateServer(today);
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        await FirebirdTestData.SeedPersonAsync(
            server.Services,
            memberId,
            name: "Private birthday",
            birthdayMonth: today.Month,
            birthdayDay: today.Day,
            visibility: RecordVisibility.Private);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        Assert.False(await FirebirdAttentionAsync(admin));
        Assert.True(await FirebirdAttentionAsync(member));
    }

    [Fact]
    public async Task Public_qualifying_person_activates_attention_for_every_user()
    {
        var today = new DateOnly(2026, 6, 21);
        using var server = CreateServer(today);
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        await FirebirdTestData.SeedPersonAsync(
            server.Services,
            memberId,
            name: "Public birthday",
            birthdayMonth: today.Month,
            birthdayDay: today.Day);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        Assert.True(await FirebirdAttentionAsync(admin));
        Assert.True(await FirebirdAttentionAsync(member));
    }

    private static CapexTestServer CreateServer(DateOnly madridToday)
    {
        var clock = new FixedClock(new DateTimeOffset(
            madridToday.Year,
            madridToday.Month,
            madridToday.Day,
            10,
            0,
            0,
            TimeSpan.FromHours(1)));

        return new CapexTestServer(configureServices: services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(clock);
        });
    }

    private static async Task<bool> FirebirdAttentionAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<LauncherAttentionResponse>(
            "/api/launcher/attention",
            CancellationToken.None);
        Assert.NotNull(response);
        var firebird = Assert.Single(response.Modules, module => module.Module == FirebirdLauncherCard.ModuleKey);
        return firebird.RequiresAttention;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow.ToUniversalTime();
    }
}

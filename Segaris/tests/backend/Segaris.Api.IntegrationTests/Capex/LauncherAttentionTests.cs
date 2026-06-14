using System.Net;
using System.Net.Http.Json;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Capex;

public sealed class LauncherAttentionTests
{
    private static readonly TimeZoneInfo Household = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    private static DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Household).Date);

    [Fact]
    public async Task Attention_requires_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/launcher/attention", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Capex_reports_no_attention_without_qualifying_entries()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        Assert.False(await CapexAttentionAsync(client));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-3, true)]
    [InlineData(3, false)]
    public async Task Planning_entries_activate_attention_only_when_due_today_or_earlier(int dayOffset, bool expected)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(
            server.Services,
            founderId,
            title: "Planning entry",
            status: CapexEntryStatus.Planning,
            dueDate: Today.AddDays(dayOffset));

        Assert.Equal(expected, await CapexAttentionAsync(client));
    }

    [Fact]
    public async Task Completed_overdue_entries_do_not_activate_attention()
    {
        Assert.False(await OverdueAttentionForStatusAsync(CapexEntryStatus.Completed));
    }

    [Fact]
    public async Task Canceled_overdue_entries_do_not_activate_attention()
    {
        Assert.False(await OverdueAttentionForStatusAsync(CapexEntryStatus.Canceled));
    }

    private static async Task<bool> OverdueAttentionForStatusAsync(CapexEntryStatus status)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(
            server.Services,
            founderId,
            title: "Non planning",
            status: status,
            dueDate: Today.AddDays(-1));

        return await CapexAttentionAsync(client);
    }

    [Fact]
    public async Task Zero_total_planning_entry_still_activates_attention()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await CapexTestData.SeedEntryAsync(
            server.Services,
            founderId,
            title: "Zero total",
            status: CapexEntryStatus.Planning,
            dueDate: Today,
            items: [new("Unknown amount", 1m, 0m)]);

        Assert.True(await CapexAttentionAsync(client));
    }

    [Fact]
    public async Task Another_users_private_overdue_entry_does_not_activate_attention_for_others()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        await CapexTestData.SeedEntryAsync(
            server.Services,
            memberId,
            title: "Private overdue",
            status: CapexEntryStatus.Planning,
            dueDate: Today.AddDays(-1),
            visibility: RecordVisibility.Private);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        // No privacy bypass for the administrator; the owning member is alerted.
        Assert.False(await CapexAttentionAsync(admin));
        Assert.True(await CapexAttentionAsync(member));
    }

    [Fact]
    public async Task Public_overdue_entry_activates_attention_for_every_user()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        await CapexTestData.SeedEntryAsync(
            server.Services,
            memberId,
            title: "Public overdue",
            status: CapexEntryStatus.Planning,
            dueDate: Today.AddDays(-1));

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        Assert.True(await CapexAttentionAsync(admin));
        Assert.True(await CapexAttentionAsync(member));
    }

    private static async Task<bool> CapexAttentionAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<LauncherAttentionResponse>(
            "/api/launcher/attention",
            CancellationToken.None);
        Assert.NotNull(response);
        var capex = Assert.Single(response.Modules, module => module.Module == CapexLauncherCard.ModuleKey);
        return capex.RequiresAttention;
    }
}

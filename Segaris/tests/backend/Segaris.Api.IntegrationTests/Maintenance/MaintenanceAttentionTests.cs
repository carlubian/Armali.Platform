using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Maintenance;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Shared.Authorization;
using Segaris.Shared.Time;

namespace Segaris.Api.IntegrationTests.Maintenance;

public sealed class MaintenanceAttentionTests
{
    [Fact]
    public async Task Maintenance_reports_no_attention_without_qualifying_tasks()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        Assert.False(await MaintenanceAttentionAsync(client));
    }

    [Theory]
    [InlineData("Pending", -3, true)]
    [InlineData("Pending", 0, true)]
    [InlineData("InProgress", 7, true)]
    [InlineData("Pending", 8, false)]
    [InlineData("Completed", -1, false)]
    [InlineData("Cancelled", -1, false)]
    public async Task Attention_matches_open_statuses_and_madrid_seven_day_window(
        string status,
        int dueOffsetDays,
        bool expected)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Tracked maintenance",
            status: Enum.Parse<MaintenanceStatus>(status),
            dueDate: Today(server.Services).AddDays(dueOffsetDays));

        Assert.Equal(expected, await MaintenanceAttentionAsync(client));
    }

    [Fact]
    public async Task Task_without_due_date_does_not_activate_attention()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Undated maintenance",
            status: MaintenanceStatus.Pending,
            dueDate: null);

        Assert.False(await MaintenanceAttentionAsync(client));
    }

    [Fact]
    public async Task Another_users_private_qualifying_task_does_not_activate_attention_for_others()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("maintenance-attention-owner", "MaintenanceAttention123!");
        await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            memberId,
            title: "Private maintenance due soon",
            dueDate: Today(server.Services).AddDays(1),
            visibility: RecordVisibility.Private);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "maintenance-attention-owner", "MaintenanceAttention123!");

        Assert.False(await MaintenanceAttentionAsync(admin));
        Assert.True(await MaintenanceAttentionAsync(member));
    }

    [Fact]
    public async Task Public_qualifying_task_activates_attention_for_every_user()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("maintenance-attention-member", "MaintenanceAttention123!");
        await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            memberId,
            title: "Public maintenance due soon",
            dueDate: Today(server.Services).AddDays(1));

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "maintenance-attention-member", "MaintenanceAttention123!");

        Assert.True(await MaintenanceAttentionAsync(admin));
        Assert.True(await MaintenanceAttentionAsync(member));
    }

    private static DateOnly Today(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        return MaintenanceCivilDate.Today(clock);
    }

    private static async Task<bool> MaintenanceAttentionAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<LauncherAttentionResponse>(
            "/api/launcher/attention",
            CancellationToken.None);
        Assert.NotNull(response);
        var maintenance = Assert.Single(response.Modules, module => module.Module == MaintenanceLauncherCard.ModuleKey);
        return maintenance.RequiresAttention;
    }
}

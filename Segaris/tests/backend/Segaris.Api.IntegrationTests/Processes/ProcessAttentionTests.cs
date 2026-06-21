using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Processes;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Shared.Authorization;
using Segaris.Shared.Time;

namespace Segaris.Api.IntegrationTests.Processes;

public sealed class ProcessAttentionTests
{
    [Fact]
    public async Task Processes_reports_no_attention_without_qualifying_processes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        Assert.False(await ProcessesAttentionAsync(client));
    }

    [Theory]
    [InlineData("global", -2, true)]
    [InlineData("global", 0, true)]
    [InlineData("global", 7, true)]
    [InlineData("global", 8, false)]
    [InlineData("frontier", -2, true)]
    [InlineData("frontier", 0, true)]
    [InlineData("frontier", 7, true)]
    [InlineData("frontier", 8, false)]
    public async Task Attention_matches_global_or_frontier_due_date_window(
        string dueDateSource,
        int dueOffsetDays,
        bool expected)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var dueDate = Today(server.Services).AddDays(dueOffsetDays);

        if (dueDateSource == "global")
        {
            await ProcessTestData.SeedProcessAsync(server.Services, founderId, dueDate: dueDate);
        }
        else
        {
            await ProcessTestData.SeedProcessAsync(
                server.Services,
                founderId,
                steps:
                [
                    new SeedStep("Completed setup", Today(server.Services).AddDays(-10), State: StepExecutionState.Completed),
                    new SeedStep("Frontier step", dueDate),
                ]);
        }

        Assert.Equal(expected, await ProcessesAttentionAsync(client));
    }

    [Fact]
    public async Task Completed_cancelled_and_undated_processes_do_not_activate_attention()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var overdue = Today(server.Services).AddDays(-1);

        await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Completed",
            dueDate: overdue,
            steps: [new SeedStep("Done", overdue, State: StepExecutionState.Completed)]);
        await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Cancelled",
            dueDate: overdue,
            isCancelled: true);
        await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Undated",
            steps: [new SeedStep("No due date")]);

        Assert.False(await ProcessesAttentionAsync(client));
    }

    [Fact]
    public async Task Only_the_next_pending_frontier_step_due_date_counts()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            steps:
            [
                new SeedStep("Frontier far future", Today(server.Services).AddDays(8)),
                new SeedStep("Later overdue", Today(server.Services).AddDays(-1)),
            ]);

        Assert.False(await ProcessesAttentionAsync(client));
    }

    [Fact]
    public async Task Accessibility_filtering_does_not_disclose_private_processes()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("process-attention-owner", "ProcessAttention123!");
        await ProcessTestData.SeedProcessAsync(
            server.Services,
            memberId,
            name: "Private due soon",
            dueDate: Today(server.Services).AddDays(1),
            visibility: RecordVisibility.Private);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "process-attention-owner", "ProcessAttention123!");

        Assert.False(await ProcessesAttentionAsync(admin));
        Assert.True(await ProcessesAttentionAsync(member));
    }

    [Fact]
    public async Task Public_qualifying_process_activates_attention_for_every_user()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("process-attention-member", "ProcessAttention123!");
        await ProcessTestData.SeedProcessAsync(
            server.Services,
            memberId,
            name: "Public due soon",
            dueDate: Today(server.Services).AddDays(1));

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "process-attention-member", "ProcessAttention123!");

        Assert.True(await ProcessesAttentionAsync(admin));
        Assert.True(await ProcessesAttentionAsync(member));
    }

    private static DateOnly Today(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        return ProcessCivilDate.Today(clock);
    }

    private static async Task<bool> ProcessesAttentionAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<LauncherAttentionResponse>(
            "/api/launcher/attention",
            CancellationToken.None);
        Assert.NotNull(response);
        var processes = Assert.Single(response.Modules, module => module.Module == ProcessesLauncherCard.ModuleKey);
        return processes.RequiresAttention;
    }
}

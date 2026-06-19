using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Maintenance;

public sealed class MaintenanceTaskWave2Tests
{
    [Fact]
    public async Task Tasks_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/maintenance/tasks", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_persists_defaults_and_trims_text()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var typeId = await MaintenanceTestData.TypeIdAsync(server.Services, "Repair");

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/maintenance/tasks",
            MaintenanceTaskRequestBuilder.Default()
                .WithTitle("  Replace air filter  ")
                .WithType(typeId)
                .WithStatus(null)
                .WithPriority(null)
                .WithNotes("  before summer  ")
                .WithVisibility(null)
                .BuildCreate(),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MaintenanceTaskResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Replace air filter", created.Title);
        Assert.Equal(typeId, created.MaintenanceTypeId);
        Assert.Equal("Repair", created.MaintenanceTypeName);
        Assert.Equal("Pending", created.Status);
        Assert.Equal("Medium", created.Priority);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal("before summer", created.Notes);
        Assert.Null(created.CompletedDate);
        Assert.Null(created.AssetId);
        Assert.Null(created.AssetName);
        Assert.Empty(created.Attachments);
    }

    [Fact]
    public async Task List_supports_pagination_search_exact_filters_and_due_date_sorting()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Boiler inspection",
            typeName: "Inspection",
            status: MaintenanceStatus.InProgress,
            priority: MaintenancePriority.High,
            dueDate: new DateOnly(2026, 2, 1),
            notes: "annual safety check");
        await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Clean extractor",
            typeName: "Cleaning",
            status: MaintenanceStatus.Pending,
            priority: MaintenancePriority.Low,
            dueDate: null,
            notes: "kitchen grease",
            visibility: RecordVisibility.Private);
        await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Repair garden gate",
            typeName: "Repair",
            status: MaintenanceStatus.Cancelled,
            priority: MaintenancePriority.Medium,
            dueDate: new DateOnly(2026, 1, 10));

        var cleaningId = await MaintenanceTestData.TypeIdAsync(server.Services, "Cleaning");

        var firstPage = await GetPageAsync(client, "/api/maintenance/tasks?page=1&pageSize=2");
        var search = await GetPageAsync(client, "/api/maintenance/tasks?search=SAFETY");
        var byType = await GetPageAsync(client, $"/api/maintenance/tasks?type={cleaningId}");
        var byStatus = await GetPageAsync(client, "/api/maintenance/tasks?status=Cancelled");
        var byPriority = await GetPageAsync(client, "/api/maintenance/tasks?priority=Low");
        var privateOnly = await GetPageAsync(client, "/api/maintenance/tasks?visibility=Private");
        var byCreator = await GetPageAsync(client, $"/api/maintenance/tasks?creator={founderId}");
        var byTypeSort = await GetPageAsync(client, "/api/maintenance/tasks?sort=type&sortDirection=asc");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(["Repair garden gate", "Boiler inspection"], firstPage.Items.Select(item => item.Title).ToArray());
        Assert.Equal("Boiler inspection", Assert.Single(search.Items).Title);
        Assert.Equal("Clean extractor", Assert.Single(byType.Items).Title);
        Assert.Equal("Repair garden gate", Assert.Single(byStatus.Items).Title);
        Assert.Equal("Clean extractor", Assert.Single(byPriority.Items).Title);
        Assert.Equal("Clean extractor", Assert.Single(privateOnly.Items).Title);
        Assert.Equal(3, byCreator.TotalCount);
        Assert.Equal(["Cleaning", "Inspection", "Repair"], byTypeSort.Items.Select(item => item.MaintenanceTypeName).ToArray());
    }

    [Fact]
    public async Task Detail_update_and_delete_manage_the_complete_task_lifecycle()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var taskId = await MaintenanceTestData.SeedTaskAsync(server.Services, founderId, title: "Original");
        var inspectionId = await MaintenanceTestData.TypeIdAsync(server.Services, "Inspection");

        var detail = await client.GetFromJsonAsync<MaintenanceTaskResponse>($"/api/maintenance/tasks/{taskId}", CancellationToken.None);
        using var complete = await CapexApi.PutJsonAsync(
            client,
            $"/api/maintenance/tasks/{taskId}",
            MaintenanceTaskRequestBuilder.Default()
                .WithTitle("Updated inspection")
                .WithType(inspectionId)
                .WithStatus("Completed")
                .WithPriority("High")
                .WithDueDate(new DateOnly(2026, 7, 1))
                .WithNotes("Done")
                .WithVisibility("Private")
                .BuildUpdate(),
            csrf);
        var completed = await complete.Content.ReadFromJsonAsync<MaintenanceTaskResponse>(CancellationToken.None);
        using var reopen = await CapexApi.PutJsonAsync(
            client,
            $"/api/maintenance/tasks/{taskId}",
            MaintenanceTaskRequestBuilder.Default()
                .WithTitle("Updated inspection")
                .WithType(inspectionId)
                .WithStatus("InProgress")
                .WithPriority("High")
                .WithDueDate(new DateOnly(2026, 7, 1))
                .WithNotes("Needs follow-up")
                .WithVisibility("Private")
                .BuildUpdate(),
            csrf);
        var reopened = await reopen.Content.ReadFromJsonAsync<MaintenanceTaskResponse>(CancellationToken.None);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/maintenance/tasks/{taskId}", csrf);

        Assert.NotNull(detail);
        Assert.Equal("Original", detail.Title);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.Equal("Updated inspection", completed!.Title);
        Assert.Equal("Completed", completed.Status);
        Assert.NotNull(completed.CompletedDate);
        Assert.Equal("Private", completed.Visibility);
        Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);
        Assert.Equal("InProgress", reopened!.Status);
        Assert.Null(reopened.CompletedDate);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await MaintenanceTestData.TaskExistsAsync(server.Services, taskId));
    }

    [Fact]
    public async Task Unknown_type_required_fields_and_invalid_values_return_task_problems()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var typeId = await MaintenanceTestData.TypeIdAsync(server.Services, "Repair");

        using var unknown = await CapexApi.PostJsonAsync(
            client,
            "/api/maintenance/tasks",
            MaintenanceTaskRequestBuilder.Default().WithType(999_999).BuildCreate(),
            csrf);
        using var invalid = await CapexApi.PostJsonAsync(
            client,
            "/api/maintenance/tasks",
            MaintenanceTaskRequestBuilder.Default().WithType(typeId).WithPriority("Urgent").BuildCreate(),
            csrf);
        using var blank = await CapexApi.PostJsonAsync(
            client,
            "/api/maintenance/tasks",
            MaintenanceTaskRequestBuilder.Default().WithType(typeId).WithTitle("   ").BuildCreate(),
            csrf);
        using var missingType = await CapexApi.PostJsonAsync(
            client,
            "/api/maintenance/tasks",
            MaintenanceTaskRequestBuilder.Default().WithTitle("Missing type").BuildCreate(),
            csrf);

        var unknownProblem = await unknown.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidProblem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var blankProblem = await blank.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var missingTypeProblem = await missingType.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal("maintenance.task.unknown_type", unknownProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("maintenance.task.validation", invalidProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal("maintenance.task.validation", blankProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, missingType.StatusCode);
        Assert.Equal("maintenance.task.validation", missingTypeProblem!.Code);
    }

    [Fact]
    public async Task Public_collaboration_and_private_isolation_follow_visibility_rules()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicTaskId = await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Shared",
            visibility: RecordVisibility.Public);
        var privateTaskId = await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Private",
            visibility: RecordVisibility.Private);
        var typeId = await MaintenanceTestData.TypeIdAsync(server.Services, "Repair");

        await server.CreateUserAsync("maintenance-member", "MaintenanceMember123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "maintenance-member", "MaintenanceMember123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var editPublic = await CapexApi.PutJsonAsync(
            member,
            $"/api/maintenance/tasks/{publicTaskId}",
            MaintenanceTaskRequestBuilder.Default().WithTitle("Shared edited").WithType(typeId).BuildUpdate(),
            memberCsrf);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/maintenance/tasks/{privateTaskId}",
            MaintenanceTaskRequestBuilder.Default().WithTitle("Private edited").WithType(typeId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);
        using var makePrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/maintenance/tasks/{publicTaskId}",
            MaintenanceTaskRequestBuilder.Default().WithTitle("Shared hidden").WithType(typeId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);
        using var getPrivate = await member.GetAsync($"/api/maintenance/tasks/{privateTaskId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, editPublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, makePrivate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getPrivate.StatusCode);
    }

    private static async Task<PaginatedResponse<MaintenanceTaskSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<MaintenanceTaskSummaryResponse>>(route, CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code);
}

using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Processes;

public sealed class ProcessEndpointTests
{
    [Fact]
    public async Task Processes_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/processes", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_persists_defaults_trims_text_and_uses_first_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var administrativeId = await ProcessTestData.CategoryIdAsync(server.Services, "Administrative");

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/processes",
            ProcessRequestBuilder.Default()
                .WithName("  Renew passport  ")
                .WithCategory(0)
                .WithNotes("  collect documents  ")
                .WithVisibility(null)
                .BuildCreate(),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Renew passport", created.Name);
        Assert.Equal(administrativeId, created.CategoryId);
        Assert.Equal("Administrative", created.CategoryName);
        Assert.Equal("NotStarted", created.Status);
        Assert.False(created.IsCancelled);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal("collect documents", created.Notes);
        Assert.Null(created.DueDate);
        Assert.Null(created.EffectiveDueDate);
        Assert.Equal(0, created.ResolvedStepCount);
        Assert.Equal(0, created.TotalStepCount);
        Assert.Null(created.NextPendingStepId);
        Assert.Empty(created.Steps);
        Assert.Empty(created.Attachments);
    }

    [Fact]
    public async Task List_supports_pagination_search_exact_filters_progress_status_and_effective_due_date_sorting()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Tax filing",
            categoryName: "Tax",
            notes: "annual declaration",
            steps:
            [
                new SeedStep("Submit draft", new DateOnly(2026, 2, 1)),
            ]);
        await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Residence permit",
            categoryName: "Administrative",
            dueDate: new DateOnly(2026, 1, 10),
            steps:
            [
                new SeedStep("Book appointment", State: StepExecutionState.Completed),
                new SeedStep("Attend appointment", new DateOnly(2026, 1, 20)),
            ]);
        await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Archive closed",
            categoryName: "Legal",
            steps:
            [
                new SeedStep("File resolution", State: StepExecutionState.Completed),
            ]);
        await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Cancelled health",
            categoryName: "Health",
            dueDate: new DateOnly(2026, 1, 5),
            isCancelled: true,
            visibility: RecordVisibility.Private);
        var taxId = await ProcessTestData.CategoryIdAsync(server.Services, "Tax");

        var firstPage = await GetPageAsync(client, "/api/processes?page=1&pageSize=2");
        var search = await GetPageAsync(client, "/api/processes?search=DECLARATION");
        var byCategory = await GetPageAsync(client, $"/api/processes?category={taxId}");
        var byStatus = await GetPageAsync(client, "/api/processes?status=InProgress");
        var byVisibility = await GetPageAsync(client, "/api/processes?visibility=Private");
        var byCreator = await GetPageAsync(client, $"/api/processes?creator={founderId}");
        var byCategorySort = await GetPageAsync(client, "/api/processes?sort=category&sortDirection=asc");

        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(["Cancelled health", "Residence permit"], firstPage.Items.Select(item => item.Name).ToArray());
        Assert.Equal("Tax filing", Assert.Single(search.Items).Name);
        var tax = Assert.Single(byCategory.Items);
        Assert.Equal("Tax filing", tax.Name);
        Assert.Equal("NotStarted", tax.Status);
        Assert.Equal(0, tax.ResolvedStepCount);
        Assert.Equal(1, tax.TotalStepCount);
        Assert.Equal(new DateOnly(2026, 2, 1), tax.EffectiveDueDate);
        var progress = Assert.Single(byStatus.Items);
        Assert.Equal("Residence permit", progress.Name);
        Assert.Equal(1, progress.ResolvedStepCount);
        Assert.Equal(2, progress.TotalStepCount);
        Assert.Equal("Cancelled health", Assert.Single(byVisibility.Items).Name);
        Assert.Equal(4, byCreator.TotalCount);
        Assert.Equal(["Administrative", "Health", "Legal", "Tax"], byCategorySort.Items.Select(item => item.CategoryName).ToArray());
    }

    [Fact]
    public async Task Detail_update_cancel_reopen_and_delete_manage_the_process_lifecycle()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var processId = await ProcessTestData.SeedProcessAsync(server.Services, founderId, name: "Original");
        var legalId = await ProcessTestData.CategoryIdAsync(server.Services, "Legal");

        var detail = await client.GetFromJsonAsync<ProcessResponse>($"/api/processes/{processId}", CancellationToken.None);
        using var update = await CapexApi.PutJsonAsync(
            client,
            $"/api/processes/{processId}",
            ProcessRequestBuilder.Default()
                .WithName("Updated legal process")
                .WithCategory(legalId)
                .WithDueDate(new DateOnly(2026, 7, 1))
                .WithNotes("Done")
                .WithVisibility("Private")
                .BuildUpdate(),
            csrf);
        var updated = await update.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None);
        using var cancel = await CapexApi.PostJsonAsync(client, $"/api/processes/{processId}/cancel", new { }, csrf);
        var cancelled = await cancel.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None);
        using var reopen = await CapexApi.PostJsonAsync(client, $"/api/processes/{processId}/reopen", new { }, csrf);
        var reopened = await reopen.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/processes/{processId}", csrf);

        Assert.NotNull(detail);
        Assert.Equal("Original", detail.Name);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Updated legal process", updated!.Name);
        Assert.Equal("Legal", updated.CategoryName);
        Assert.Equal(new DateOnly(2026, 7, 1), updated.DueDate);
        Assert.Equal("Private", updated.Visibility);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.True(cancelled!.IsCancelled);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);
        Assert.False(reopened!.IsCancelled);
        Assert.Equal("NotStarted", reopened.Status);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await ProcessTestData.ProcessExistsAsync(server.Services, processId));
    }

    [Fact]
    public async Task Unknown_category_required_fields_invalid_values_and_bad_query_values_return_problems()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await ProcessTestData.CategoryIdAsync(server.Services, "Administrative");

        using var unknown = await CapexApi.PostJsonAsync(
            client,
            "/api/processes",
            ProcessRequestBuilder.Default().WithCategory(999_999).BuildCreate(),
            csrf);
        using var blank = await CapexApi.PostJsonAsync(
            client,
            "/api/processes",
            ProcessRequestBuilder.Default().WithCategory(categoryId).WithName("   ").BuildCreate(),
            csrf);
        using var invalidVisibility = await CapexApi.PostJsonAsync(
            client,
            "/api/processes",
            ProcessRequestBuilder.Default().WithCategory(categoryId).WithVisibility("Shared").BuildCreate(),
            csrf);
        using var invalidStatus = await client.GetAsync("/api/processes?status=Paused", CancellationToken.None);

        var unknownProblem = await unknown.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var blankProblem = await blank.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var visibilityProblem = await invalidVisibility.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var statusProblem = await invalidStatus.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal("processes.process.unknown_category", unknownProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal("processes.process.validation", blankProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalidVisibility.StatusCode);
        Assert.Equal("processes.process.validation", visibilityProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        Assert.Equal("request.invalid", statusProblem!.Code);
    }

    [Fact]
    public async Task Public_collaboration_and_private_isolation_follow_visibility_rules()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicProcessId = await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Shared",
            visibility: RecordVisibility.Public);
        var privateProcessId = await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        var categoryId = await ProcessTestData.CategoryIdAsync(server.Services, "Administrative");

        await server.CreateUserAsync("process-member", "ProcessMember123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "process-member", "ProcessMember123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var editPublic = await CapexApi.PutJsonAsync(
            member,
            $"/api/processes/{publicProcessId}",
            ProcessRequestBuilder.Default().WithName("Shared edited").WithCategory(categoryId).BuildUpdate(),
            memberCsrf);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/processes/{privateProcessId}",
            ProcessRequestBuilder.Default().WithName("Private edited").WithCategory(categoryId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);
        using var makePrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/processes/{publicProcessId}",
            ProcessRequestBuilder.Default().WithName("Shared hidden").WithCategory(categoryId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);
        using var getPrivate = await member.GetAsync($"/api/processes/{privateProcessId}", CancellationToken.None);
        var memberPage = await GetPageAsync(member, "/api/processes");

        Assert.Equal(HttpStatusCode.OK, editPublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, makePrivate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getPrivate.StatusCode);
        Assert.DoesNotContain(memberPage.Items, item => item.Name == "Private");
    }

    private static async Task<PaginatedResponse<ProcessSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<ProcessSummaryResponse>>(route, CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code);
}

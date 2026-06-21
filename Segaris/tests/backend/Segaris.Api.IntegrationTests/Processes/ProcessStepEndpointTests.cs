using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Processes;

public sealed class ProcessStepEndpointTests
{
    [Fact]
    public async Task Step_restructure_adds_removes_reorders_edits_and_preserves_execution_state()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var processId = await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            steps:
            [
                new SeedStep("Gather documents", State: StepExecutionState.Completed),
                new SeedStep("Old second"),
                new SeedStep("Book appointment", new DateOnly(2026, 6, 10), Notes: "old"),
            ]);
        var original = await GetStepsAsync(client, processId);
        var completed = original.Single(step => step.Description == "Gather documents");
        var moved = original.Single(step => step.Description == "Book appointment");

        using var response = await CapexApi.PutJsonAsync(
            client,
            $"/api/processes/{processId}/steps",
            new UpdateStepListRequest(
            [
                new StepListItemRequest(completed.Id, "  Gather all documents  ", new DateOnly(2026, 6, 1), "  updated  ", IsOptional: false),
                new StepListItemRequest(moved.Id, "Book appointment online", new DateOnly(2026, 6, 20), null, IsOptional: true),
                new StepListItemRequest(null, "Attend appointment", null, "bring ID", IsOptional: false),
            ]),
            csrf);
        var process = await response.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None);
        var steps = await GetStepsAsync(client, processId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(process);
        Assert.Equal("InProgress", process.Status);
        Assert.Equal(1, process.ResolvedStepCount);
        Assert.Equal(3, process.TotalStepCount);
        Assert.Equal(moved.Id, process.NextPendingStepId);
        Assert.Equal(["Gather all documents", "Book appointment online", "Attend appointment"], steps.Select(step => step.Description).ToArray());
        Assert.Equal(["Completed", "Pending", "Pending"], steps.Select(step => step.State).ToArray());
        Assert.Equal([0, 1, 2], steps.Select(step => step.SortOrder).ToArray());
        Assert.Equal(new DateOnly(2026, 6, 1), steps[0].DueDate);
        Assert.Equal("updated", steps[0].Notes);
        Assert.True(steps[1].IsOptional);
        Assert.DoesNotContain(steps, step => step.Description == "Old second");
    }

    [Fact]
    public async Task Restructure_rejects_pending_insertions_inside_the_resolved_prefix_and_resolved_steps_after_pending()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var processId = await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            steps:
            [
                new SeedStep("Resolved", State: StepExecutionState.Completed),
                new SeedStep("Pending"),
            ]);
        var steps = await GetStepsAsync(client, processId);
        var resolved = steps.Single(step => step.Description == "Resolved");
        var pending = steps.Single(step => step.Description == "Pending");

        using var newBeforeResolved = await CapexApi.PutJsonAsync(
            client,
            $"/api/processes/{processId}/steps",
            new UpdateStepListRequest(
            [
                new StepListItemRequest(null, "Inserted too early", null, null, IsOptional: false),
                ToRequest(resolved),
                ToRequest(pending),
            ]),
            csrf);
        using var resolvedAfterPending = await CapexApi.PutJsonAsync(
            client,
            $"/api/processes/{processId}/steps",
            new UpdateStepListRequest(
            [
                ToRequest(pending),
                ToRequest(resolved),
            ]),
            csrf);
        var firstProblem = await newBeforeResolved.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var secondProblem = await resolvedAfterPending.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, newBeforeResolved.StatusCode);
        Assert.Equal("processes.step.contiguity_violation", firstProblem!.Code);
        Assert.Equal(HttpStatusCode.Conflict, resolvedAfterPending.StatusCode);
        Assert.Equal("processes.step.contiguity_violation", secondProblem!.Code);
    }

    [Fact]
    public async Task Frontier_actions_complete_skip_and_undo_in_order_and_reject_invalid_actions()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var processId = await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            steps:
            [
                new SeedStep("Required first"),
                new SeedStep("Optional second", IsOptional: true),
                new SeedStep("Required third"),
            ]);
        var steps = await GetStepsAsync(client, processId);
        var first = steps[0];
        var second = steps[1];
        var third = steps[2];

        using var completeNonFrontier = await CapexApi.PostJsonAsync(client, $"/api/processes/{processId}/steps/{second.Id}/complete", new { }, csrf);
        using var skipRequired = await CapexApi.PostJsonAsync(client, $"/api/processes/{processId}/steps/{first.Id}/skip", new { }, csrf);
        using var undoBeforeResolved = await CapexApi.PostJsonAsync(client, $"/api/processes/{processId}/steps/{first.Id}/undo", new { }, csrf);
        using var completeFirst = await CapexApi.PostJsonAsync(client, $"/api/processes/{processId}/steps/{first.Id}/complete", new { }, csrf);
        var afterFirst = await completeFirst.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None);
        using var skipSecond = await CapexApi.PostJsonAsync(client, $"/api/processes/{processId}/steps/{second.Id}/skip", new { }, csrf);
        var afterSecond = await skipSecond.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None);
        using var completeThird = await CapexApi.PostJsonAsync(client, $"/api/processes/{processId}/steps/{third.Id}/complete", new { }, csrf);
        var completed = await completeThird.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None);
        using var undoThird = await CapexApi.PostJsonAsync(client, $"/api/processes/{processId}/steps/{third.Id}/undo", new { }, csrf);
        var afterUndo = await undoThird.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None);
        var finalSteps = await GetStepsAsync(client, processId);

        Assert.Equal(HttpStatusCode.Conflict, completeNonFrontier.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, skipRequired.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, undoBeforeResolved.StatusCode);
        Assert.Equal(HttpStatusCode.OK, completeFirst.StatusCode);
        Assert.Equal("InProgress", afterFirst!.Status);
        Assert.Equal(second.Id, afterFirst.NextPendingStepId);
        Assert.Equal(HttpStatusCode.OK, skipSecond.StatusCode);
        Assert.Equal(2, afterSecond!.ResolvedStepCount);
        Assert.Equal(third.Id, afterSecond.NextPendingStepId);
        Assert.Equal(HttpStatusCode.OK, completeThird.StatusCode);
        Assert.Equal("Completed", completed!.Status);
        Assert.Null(completed.NextPendingStepId);
        Assert.Equal(HttpStatusCode.OK, undoThird.StatusCode);
        Assert.Equal("InProgress", afterUndo!.Status);
        Assert.Equal(third.Id, afterUndo.NextPendingStepId);
        Assert.Equal(["Completed", "Skipped", "Pending"], finalSteps.Select(step => step.State).ToArray());
    }

    [Fact]
    public async Task Step_access_inherits_public_collaboration_and_private_isolation()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicProcessId = await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Shared",
            visibility: RecordVisibility.Public,
            steps: [new SeedStep("Shared step")]);
        var privateProcessId = await ProcessTestData.SeedProcessAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private,
            steps: [new SeedStep("Private step")]);

        await server.CreateUserAsync("process-steps-member", "ProcessSteps123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "process-steps-member", "ProcessSteps123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var publicSteps = await GetStepsAsync(member, publicProcessId);

        using var completePublic = await CapexApi.PostJsonAsync(
            member,
            $"/api/processes/{publicProcessId}/steps/{publicSteps[0].Id}/complete",
            new { },
            memberCsrf);
        using var getPrivate = await member.GetAsync($"/api/processes/{privateProcessId}/steps", CancellationToken.None);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/processes/{privateProcessId}/steps",
            new UpdateStepListRequest([new StepListItemRequest(null, "Hidden", null, null, IsOptional: false)]),
            memberCsrf);

        Assert.Equal(HttpStatusCode.OK, completePublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getPrivate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
    }

    private static async Task<IReadOnlyList<StepResponse>> GetStepsAsync(HttpClient client, int processId)
    {
        var steps = await client.GetFromJsonAsync<IReadOnlyList<StepResponse>>($"/api/processes/{processId}/steps", CancellationToken.None);
        Assert.NotNull(steps);
        return steps;
    }

    private static StepListItemRequest ToRequest(StepResponse step) => new(
        step.Id,
        step.Description,
        step.DueDate,
        step.Notes,
        step.IsOptional);

    private sealed record ProblemPayload(string? Code);
}

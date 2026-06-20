using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Projects.Contracts;

namespace Segaris.Api.IntegrationTests.Projects;

public sealed class ProjectsItemEndpointTests
{
    [Fact]
    public async Task Tree_reads_and_mutations_apply_defaults_ordering_and_identifier_recomputation()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var program = await CreateProgramAsync(client, csrf, "Program", "PROG");
        var axis = await CreateAxisAsync(client, csrf, program.Id, "Axis", "AXIS");
        var targetAxis = await CreateAxisAsync(client, csrf, program.Id, "Moved", "MOVE");

        var project = await CreateProjectAsync(client, csrf, new CreateProjectRequest(axis.Id, "  Project one  ", null, null));
        var activity = await CreateActivityAsync(client, csrf, new CreateActivityRequest(axis.Id, "Activity one", "Active", "Public"));

        Assert.Equal(("Project one", "Planning", "Public", 1, $"PROGAXIS-000001 Project one"), (project.Name, project.Status, project.Visibility, project.Number, project.Identifier));
        Assert.Equal(("Activity one", "Active", "Public", 2, $"PROGAXIS-000002 Activity one"), (activity.Name, activity.Status, activity.Visibility, activity.Number, activity.Identifier));

        var programs = await client.GetFromJsonAsync<ProgramNodeResponse[]>("/api/projects/tree/programs", CancellationToken.None);
        Assert.Equal([new ProgramNodeResponse(program.Id, "PROG", "Program")], programs!);

        var axes = await client.GetFromJsonAsync<AxisNodeResponse[]>($"/api/projects/tree/programs/{program.Id}/axes", CancellationToken.None);
        Assert.Equal(["AXIS", "MOVE"], axes!.Select(value => value.Code).ToArray());

        var items = await client.GetFromJsonAsync<ProjectTreeItemResponse[]>($"/api/projects/tree/axes/{axis.Id}/items", CancellationToken.None);
        Assert.Equal([project.Number, activity.Number], items!.Select(value => value.Number).ToArray());
        Assert.Equal(["Project", "Activity"], items!.Select(value => value.Kind).ToArray());
        Assert.Equal(new ProjectRiskBandSummaryResponse(0, 0, 0), items![0].RiskSummary);
        Assert.Null(items![1].RiskSummary);

        using var updateResponse = await CapexApi.PutJsonAsync(
            client,
            $"/api/projects/projects/{project.Id}",
            new UpdateProjectRequest(targetAxis.Id, "Moved project", "Completed", "Public"),
            csrf);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProjectResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(project.Number, updated!.Number);
        Assert.Equal($"PROGMOVE-000001 Moved project", updated.Identifier);
        Assert.Equal(targetAxis.Id, updated.AxisId);

        var sourceItems = await client.GetFromJsonAsync<ProjectTreeItemResponse[]>($"/api/projects/tree/axes/{axis.Id}/items", CancellationToken.None);
        var targetItems = await client.GetFromJsonAsync<ProjectTreeItemResponse[]>($"/api/projects/tree/axes/{targetAxis.Id}/items", CancellationToken.None);
        Assert.Equal([activity.Id], sourceItems!.Select(value => value.Id).ToArray());
        Assert.Equal([project.Id], targetItems!.Select(value => value.Id).ToArray());
    }

    [Fact]
    public async Task Visibility_filtering_preserves_public_collaboration_and_private_isolation()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        var program = await CreateProgramAsync(admin, adminCsrf, "Program", "PROG");
        var axis = await CreateAxisAsync(admin, adminCsrf, program.Id, "Axis", "AXIS");
        await server.CreateUserAsync("projects-owner", "OwnerPass123!");
        await server.CreateUserAsync("projects-collaborator", "CollaboratorPass123!");
        using var owner = await server.CreateAuthenticatedClientAsync("projects-owner", "OwnerPass123!");
        using var collaborator = await server.CreateAuthenticatedClientAsync("projects-collaborator", "CollaboratorPass123!");
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(owner);
        var collaboratorCsrf = await CapexTestServer.GetCsrfTokenAsync(collaborator);

        var privateProject = await CreateProjectAsync(owner, ownerCsrf, new CreateProjectRequest(axis.Id, "Private project", null, "Private"));
        var privateActivity = await CreateActivityAsync(owner, ownerCsrf, new CreateActivityRequest(axis.Id, "Private activity", null, "Private"));
        var collaboratorItemsBeforePublicItem = await collaborator.GetFromJsonAsync<ProjectTreeItemResponse[]>($"/api/projects/tree/axes/{axis.Id}/items", CancellationToken.None);

        Assert.Empty(collaboratorItemsBeforePublicItem!);
        using var privateGet = await collaborator.GetAsync($"/api/projects/projects/{privateProject.Id}", CancellationToken.None);
        using var privateActivityGet = await collaborator.GetAsync($"/api/projects/activities/{privateActivity.Id}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, privateGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, privateActivityGet.StatusCode);

        var publicProject = await CreateProjectAsync(owner, ownerCsrf, new CreateProjectRequest(axis.Id, "Public project", null, "Public"));
        using var collaboratorUpdate = await CapexApi.PutJsonAsync(
            collaborator,
            $"/api/projects/projects/{publicProject.Id}",
            new UpdateProjectRequest(axis.Id, "Collaborative update", "Active", "Public"),
            collaboratorCsrf);
        var updated = await collaboratorUpdate.Content.ReadFromJsonAsync<ProjectResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, collaboratorUpdate.StatusCode);
        Assert.Equal(("Collaborative update", "Active"), (updated!.Name, updated.Status));

        using var visibilityChange = await CapexApi.PutJsonAsync(
            collaborator,
            $"/api/projects/projects/{publicProject.Id}",
            new UpdateProjectRequest(axis.Id, "Collaborative update", "Active", "Private"),
            collaboratorCsrf);
        var visibilityProblem = await visibilityChange.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, visibilityChange.StatusCode);
        Assert.Equal("projects.project.visibility_forbidden", visibilityProblem!.Code);
    }

    [Fact]
    public async Task Mutation_validation_and_privacy_safe_not_found_behaviour_are_enforced()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var program = await CreateProgramAsync(client, csrf, "Program", "PROG");
        var axis = await CreateAxisAsync(client, csrf, program.Id, "Axis", "AXIS");

        using var invalidProjectName = await CapexApi.PostJsonAsync(
            client,
            "/api/projects/projects",
            new CreateProjectRequest(axis.Id, "   ", null, null),
            csrf);
        var invalidProjectProblem = await invalidProjectName.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, invalidProjectName.StatusCode);
        Assert.Equal("projects.project.validation", invalidProjectProblem!.Code);

        using var invalidActivityStatus = await CapexApi.PostJsonAsync(
            client,
            "/api/projects/activities",
            new CreateActivityRequest(axis.Id, "Activity", "Unknown", null),
            csrf);
        var invalidActivityProblem = await invalidActivityStatus.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, invalidActivityStatus.StatusCode);
        Assert.Equal("projects.activity.validation", invalidActivityProblem!.Code);

        using var missingAxis = await CapexApi.PostJsonAsync(
            client,
            "/api/projects/projects",
            new CreateProjectRequest(999, "Project", null, null),
            csrf);
        var missingAxisProblem = await missingAxis.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, missingAxis.StatusCode);
        Assert.Equal("projects.axis.not_found", missingAxisProblem!.Code);

        using var missingProject = await CapexApi.DeleteAsync(client, "/api/projects/projects/999", csrf);
        using var missingActivity = await CapexApi.DeleteAsync(client, "/api/projects/activities/999", csrf);
        Assert.Equal(HttpStatusCode.NotFound, missingProject.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingActivity.StatusCode);
    }

    private static async Task<ProgramResponse> CreateProgramAsync(HttpClient client, string csrf, string name, string code)
    {
        using var response = await CapexApi.PostJsonAsync(client, "/api/projects/programs", new ProgramRequest(name, code), csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProgramResponse>(CancellationToken.None))!;
    }

    private static async Task<AxisResponse> CreateAxisAsync(HttpClient client, string csrf, int programId, string name, string code)
    {
        using var response = await CapexApi.PostJsonAsync(client, "/api/projects/axes", new AxisRequest(name, code, programId), csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AxisResponse>(CancellationToken.None))!;
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, string csrf, CreateProjectRequest request)
    {
        using var response = await CapexApi.PostJsonAsync(client, "/api/projects/projects", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>(CancellationToken.None))!;
    }

    private static async Task<ActivityResponse> CreateActivityAsync(HttpClient client, string csrf, CreateActivityRequest request)
    {
        using var response = await CapexApi.PostJsonAsync(client, "/api/projects/activities", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ActivityResponse>(CancellationToken.None))!;
    }

    private sealed record ProblemPayload(string? Code);
}

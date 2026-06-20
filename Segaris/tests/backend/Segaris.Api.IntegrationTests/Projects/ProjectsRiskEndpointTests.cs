using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Persistence;

namespace Segaris.Api.IntegrationTests.Projects;

public sealed class ProjectsRiskEndpointTests
{
    [Fact]
    public async Task Risk_crud_computes_scores_bands_and_project_summaries()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var program = await CreateProgramAsync(client, csrf, "Program", "PROG");
        var axis = await CreateAxisAsync(client, csrf, program.Id, "Axis", "AXIS");
        var project = await CreateProjectAsync(client, csrf, new CreateProjectRequest(axis.Id, "Project one", null, null));

        var low = await CreateRiskAsync(client, csrf, project.Id, new ProjectRiskRequest("Low risk", 1, 1, 1));
        var medium = await CreateRiskAsync(client, csrf, project.Id, new ProjectRiskRequest("Medium risk", 3, 4, 5));
        var high = await CreateRiskAsync(client, csrf, project.Id, new ProjectRiskRequest("High risk", 5, 5, 4));

        Assert.Equal(("Low", 1), (low.Band, low.Score));
        Assert.Equal(("Medium", 60), (medium.Band, medium.Score));
        Assert.Equal(("High", 100), (high.Band, high.Score));

        var risks = await client.GetFromJsonAsync<ProjectRiskResponse[]>($"/api/projects/projects/{project.Id}/risks", CancellationToken.None);
        Assert.Equal([low.Id, medium.Id, high.Id], risks!.Select(risk => risk.Id).ToArray());

        var projectWithSummary = await client.GetFromJsonAsync<ProjectResponse>($"/api/projects/projects/{project.Id}", CancellationToken.None);
        Assert.Equal(new ProjectRiskBandSummaryResponse(1, 1, 1), projectWithSummary!.RiskSummary);

        var items = await client.GetFromJsonAsync<ProjectTreeItemResponse[]>($"/api/projects/tree/axes/{axis.Id}/items", CancellationToken.None);
        Assert.Equal(new ProjectRiskBandSummaryResponse(1, 1, 1), items!.Single().RiskSummary);

        using var update = await CapexApi.PutJsonAsync(
            client,
            $"/api/projects/projects/{project.Id}/risks/{medium.Id}",
            new ProjectRiskRequest("Updated high risk", 5, 5, 5),
            csrf);
        var updated = await update.Content.ReadFromJsonAsync<ProjectRiskResponse>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(("Updated high risk", 125, "High"), (updated!.Description, updated.Score, updated.Band));

        using var delete = await CapexApi.DeleteAsync(client, $"/api/projects/projects/{project.Id}/risks/{low.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var summaryAfterDelete = await client.GetFromJsonAsync<ProjectResponse>($"/api/projects/projects/{project.Id}", CancellationToken.None);
        Assert.Equal(new ProjectRiskBandSummaryResponse(0, 0, 2), summaryAfterDelete!.RiskSummary);
    }

    [Fact]
    public async Task Risk_validation_rejects_invalid_values_and_client_supplied_score()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var program = await CreateProgramAsync(client, csrf, "Program", "PROG");
        var axis = await CreateAxisAsync(client, csrf, program.Id, "Axis", "AXIS");
        var project = await CreateProjectAsync(client, csrf, new CreateProjectRequest(axis.Id, "Project one", null, null));

        using var invalidFactor = await CapexApi.PostJsonAsync(
            client,
            $"/api/projects/projects/{project.Id}/risks",
            new ProjectRiskRequest("Invalid", 0, 1, 1),
            csrf);
        var invalidProblem = await invalidFactor.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, invalidFactor.StatusCode);
        Assert.Equal("projects.risk.validation", invalidProblem!.Code);

        using var scoreSupplied = await CapexApi.PostJsonAsync(
            client,
            $"/api/projects/projects/{project.Id}/risks",
            new { description = "Invalid", probability = 1, impact = 1, mitigation = 1, score = 999 },
            csrf);
        var scoreProblem = await scoreSupplied.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, scoreSupplied.StatusCode);
        Assert.Equal("projects.risk.validation", scoreProblem!.Code);

        using var missingRisk = await CapexApi.PutJsonAsync(
            client,
            $"/api/projects/projects/{project.Id}/risks/999",
            new ProjectRiskRequest("Missing", 1, 1, 1),
            csrf);
        var missingProblem = await missingRisk.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, missingRisk.StatusCode);
        Assert.Equal("projects.risk.not_found", missingProblem!.Code);
    }

    [Fact]
    public async Task Risks_inherit_project_visibility_and_are_deleted_with_project()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        var program = await CreateProgramAsync(admin, adminCsrf, "Program", "PROG");
        var axis = await CreateAxisAsync(admin, adminCsrf, program.Id, "Axis", "AXIS");
        await server.CreateUserAsync("projects-risk-owner", "OwnerPass123!");
        await server.CreateUserAsync("projects-risk-collaborator", "CollaboratorPass123!");
        using var owner = await server.CreateAuthenticatedClientAsync("projects-risk-owner", "OwnerPass123!");
        using var collaborator = await server.CreateAuthenticatedClientAsync("projects-risk-collaborator", "CollaboratorPass123!");
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(owner);
        var collaboratorCsrf = await CapexTestServer.GetCsrfTokenAsync(collaborator);

        var privateProject = await CreateProjectAsync(owner, ownerCsrf, new CreateProjectRequest(axis.Id, "Private project", null, "Private"));
        var privateRisk = await CreateRiskAsync(owner, ownerCsrf, privateProject.Id, new ProjectRiskRequest("Private risk", 1, 1, 1));

        using var collaboratorPrivateList = await collaborator.GetAsync($"/api/projects/projects/{privateProject.Id}/risks", CancellationToken.None);
        var privateProblem = await collaboratorPrivateList.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, collaboratorPrivateList.StatusCode);
        Assert.Equal("projects.project.not_found", privateProblem!.Code);

        var publicProject = await CreateProjectAsync(owner, ownerCsrf, new CreateProjectRequest(axis.Id, "Public project", null, "Public"));
        var collaboratorRisk = await CreateRiskAsync(collaborator, collaboratorCsrf, publicProject.Id, new ProjectRiskRequest("Collaborative risk", 5, 5, 5));
        Assert.Equal(125, collaboratorRisk.Score);

        using var deleteProject = await CapexApi.DeleteAsync(owner, $"/api/projects/projects/{privateProject.Id}", ownerCsrf);
        Assert.Equal(HttpStatusCode.NoContent, deleteProject.StatusCode);

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.False(await database.Set<ProjectRisk>().AnyAsync(risk => risk.Id == privateRisk.Id, CancellationToken.None));
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

    private static async Task<ProjectRiskResponse> CreateRiskAsync(HttpClient client, string csrf, int projectId, ProjectRiskRequest request)
    {
        using var response = await CapexApi.PostJsonAsync(client, $"/api/projects/projects/{projectId}/risks", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProjectRiskResponse>(CancellationToken.None))!;
    }

    private sealed record ProblemPayload(string? Code);
}

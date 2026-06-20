using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Projects;

public sealed class ProjectsStructureEndpointTests
{
    [Theory]
    [InlineData("/api/projects/programs/1/deletion-impact")]
    [InlineData("/api/projects/axes/1/deletion-impact")]
    public async Task Management_routes_reject_normal_users(string route)
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("projects-member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("projects-member", "MemberPass123!");

        using var response = await client.GetAsync(route, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Program_and_axis_crud_validates_codes_orders_reads_and_deletes_empty_nodes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var bbbb = await CreateProgramAsync(client, csrf, "  Backlog  ", "BBBB");
        var aaaa = await CreateProgramAsync(client, csrf, "Archive", "AAAA");

        var programs = await client.GetFromJsonAsync<ProgramResponse[]>("/api/projects/programs", CancellationToken.None);
        Assert.Equal(["AAAA", "BBBB"], programs!.Select(program => program.Code).ToArray());
        Assert.Equal("Backlog", bbbb.Name);

        using var duplicateProgram = await CapexApi.PostJsonAsync(
            client,
            "/api/projects/programs",
            new ProgramRequest("Duplicate", "BBBB"),
            csrf);
        var duplicateProblem = await duplicateProgram.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Conflict, duplicateProgram.StatusCode);
        Assert.Equal("projects.program.duplicate_code", duplicateProblem!.Code);

        using var invalidProgram = await CapexApi.PostJsonAsync(
            client,
            "/api/projects/programs",
            new ProgramRequest("Invalid", "bbbb"),
            csrf);
        var invalidProblem = await invalidProgram.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, invalidProgram.StatusCode);
        Assert.Equal("projects.program.validation", invalidProblem!.Code);

        using var updatedProgramResponse = await CapexApi.PutJsonAsync(
            client,
            $"/api/projects/programs/{bbbb.Id}",
            new ProgramRequest("Current backlog", "CCCC"),
            csrf);
        var updatedProgram = await updatedProgramResponse.Content.ReadFromJsonAsync<ProgramResponse>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, updatedProgramResponse.StatusCode);
        Assert.Equal(("CCCC", "Current backlog"), (updatedProgram!.Code, updatedProgram.Name));

        var zzzz = await CreateAxisAsync(client, csrf, aaaa.Id, "Later", "ZZZZ");
        var mmmm = await CreateAxisAsync(client, csrf, aaaa.Id, "Middle", "MMMM");
        var axes = await client.GetFromJsonAsync<AxisResponse[]>("/api/projects/axes", CancellationToken.None);
        Assert.Equal(["MMMM", "ZZZZ"], axes!.Select(axis => axis.Code).ToArray());

        using var duplicateAxis = await CapexApi.PostJsonAsync(
            client,
            "/api/projects/axes",
            new AxisRequest("Duplicate", "MMMM", aaaa.Id),
            csrf);
        var duplicateAxisProblem = await duplicateAxis.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Conflict, duplicateAxis.StatusCode);
        Assert.Equal("projects.axis.duplicate_code", duplicateAxisProblem!.Code);

        using var updatedAxisResponse = await CapexApi.PutJsonAsync(
            client,
            $"/api/projects/axes/{zzzz.Id}",
            new AxisRequest("Moved", "YYYY", bbbb.Id),
            csrf);
        var updatedAxis = await updatedAxisResponse.Content.ReadFromJsonAsync<AxisResponse>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, updatedAxisResponse.StatusCode);
        Assert.Equal(("YYYY", "Moved", bbbb.Id), (updatedAxis!.Code, updatedAxis.Name, updatedAxis.ProgramId));

        using var deletedAxis = await CapexApi.DeleteAsync(client, $"/api/projects/axes/{mmmm.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deletedAxis.StatusCode);

        using var deletedProgram = await CapexApi.DeleteAsync(client, $"/api/projects/programs/{aaaa.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deletedProgram.StatusCode);
    }

    [Fact]
    public async Task Program_reassignment_moves_axes_and_deletes_the_source_atomically()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var source = await CreateProgramAsync(client, csrf, "Source", "SRCE");
        var target = await CreateProgramAsync(client, csrf, "Target", "TRGT");
        var axis = await CreateAxisAsync(client, csrf, source.Id, "Axis", "AXIS");

        using var directDelete = await CapexApi.DeleteAsync(client, $"/api/projects/programs/{source.Id}", csrf);
        var directDeleteProblem = await directDelete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Conflict, directDelete.StatusCode);
        Assert.Equal("projects.structure.reassignment_required", directDeleteProblem!.Code);

        var impact = await client.GetFromJsonAsync<StructuralNodeDeletionImpactResponse>(
            $"/api/projects/programs/{source.Id}/deletion-impact",
            CancellationToken.None);
        Assert.Equal(new StructuralNodeDeletionImpactResponse(1, HasCompatibleTarget: true), impact);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/projects/programs/{source.Id}/reassign-and-delete",
            new StructuralNodeReassignmentRequest(target.Id),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var storedAxis = await database.Set<ProjectAxis>().SingleAsync(value => value.Id == axis.Id);
        Assert.Equal(target.Id, storedAxis.ProgramId);
        Assert.Equal(adminId, storedAxis.UpdatedBy);
        Assert.False(await database.Set<ProjectProgram>().AnyAsync(value => value.Id == source.Id));
    }

    [Fact]
    public async Task Axis_reassignment_moves_mixed_projects_and_activities_without_disclosing_private_items()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("private-project-owner", "MemberPass123!");
        var (sourceAxisId, targetAxisId, projectId, activityId) = await SeedAxisWithItemsAsync(server, adminId, memberId);
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var impact = await client.GetFromJsonAsync<StructuralNodeDeletionImpactResponse>(
            $"/api/projects/axes/{sourceAxisId}/deletion-impact",
            CancellationToken.None);
        Assert.Equal(new StructuralNodeDeletionImpactResponse(2, HasCompatibleTarget: true), impact);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/projects/axes/{sourceAxisId}/reassign-and-delete",
            new StructuralNodeReassignmentRequest(targetAxisId),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var project = await database.Set<Project>().SingleAsync(value => value.Id == projectId);
        var activity = await database.Set<Activity>().SingleAsync(value => value.Id == activityId);
        Assert.Equal(targetAxisId, project.AxisId);
        Assert.Equal(targetAxisId, activity.AxisId);
        Assert.Equal(adminId, project.UpdatedBy);
        Assert.Equal(adminId, activity.UpdatedBy);
        Assert.False(await database.Set<ProjectAxis>().AnyAsync(value => value.Id == sourceAxisId));
    }

    [Fact]
    public async Task Reassignment_blocks_when_no_target_exists_and_invalid_target_rolls_back()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var (programId, sourceAxisId, projectId) = await SeedSingleAxisWithProjectAsync(server, adminId);
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var impact = await client.GetFromJsonAsync<StructuralNodeDeletionImpactResponse>(
            $"/api/projects/axes/{sourceAxisId}/deletion-impact",
            CancellationToken.None);
        Assert.Equal(new StructuralNodeDeletionImpactResponse(1, HasCompatibleTarget: false), impact);

        using var noTarget = await CapexApi.PostJsonAsync(
            client,
            $"/api/projects/axes/{sourceAxisId}/reassign-and-delete",
            new StructuralNodeReassignmentRequest(999),
            csrf);
        var noTargetProblem = await noTarget.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Conflict, noTarget.StatusCode);
        Assert.Equal("projects.structure.no_compatible_target", noTargetProblem!.Code);

        var targetAxis = await CreateAxisAsync(client, csrf, programId, "Target", "TARG");
        using var invalidTarget = await CapexApi.PostJsonAsync(
            client,
            $"/api/projects/axes/{sourceAxisId}/reassign-and-delete",
            new StructuralNodeReassignmentRequest(999),
            csrf);
        var invalidTargetProblem = await invalidTarget.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, invalidTarget.StatusCode);
        Assert.Equal("projects.structure.invalid_target", invalidTargetProblem!.Code);

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.True(await database.Set<ProjectAxis>().AnyAsync(axis => axis.Id == sourceAxisId));
        Assert.True(await database.Set<ProjectAxis>().AnyAsync(axis => axis.Id == targetAxis.Id));
        Assert.Equal(sourceAxisId, await database.Set<Project>().Where(project => project.Id == projectId).Select(project => project.AxisId).SingleAsync());
    }

    private static async Task<ProgramResponse> CreateProgramAsync(
        HttpClient client,
        string csrf,
        string name,
        string code)
    {
        using var response = await CapexApi.PostJsonAsync(client, "/api/projects/programs", new ProgramRequest(name, code), csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProgramResponse>(CancellationToken.None))!;
    }

    private static async Task<AxisResponse> CreateAxisAsync(
        HttpClient client,
        string csrf,
        int programId,
        string name,
        string code)
    {
        using var response = await CapexApi.PostJsonAsync(client, "/api/projects/axes", new AxisRequest(name, code, programId), csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AxisResponse>(CancellationToken.None))!;
    }

    private static async Task<(int SourceAxisId, int TargetAxisId, int ProjectId, int ActivityId)> SeedAxisWithItemsAsync(
        CapexTestServer server,
        int adminId,
        int memberId)
    {
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var now = DateTimeOffset.UtcNow;
        var actor = new UserId(adminId);
        var program = ProjectProgram.Create("Program", "PROG", actor, now);
        database.Add(program);
        await database.SaveChangesAsync();
        var sourceAxis = ProjectAxis.Create(program.Id, "Source", "SRCE", actor, now);
        var targetAxis = ProjectAxis.Create(program.Id, "Target", "TRGT", actor, now);
        database.AddRange(sourceAxis, targetAxis);
        await database.SaveChangesAsync();
        var project = Project.Create(
            new ProjectItemValues(sourceAxis.Id, "Private project", ProjectStatus.Planning, RecordVisibility.Private),
            1,
            new UserId(memberId),
            now);
        var activity = Activity.Create(
            new ProjectItemValues(sourceAxis.Id, "Public activity", ProjectStatus.Active, RecordVisibility.Public),
            2,
            actor,
            now);
        database.AddRange(project, activity);
        await database.SaveChangesAsync();
        return (sourceAxis.Id, targetAxis.Id, project.Id, activity.Id);
    }

    private static async Task<(int ProgramId, int SourceAxisId, int ProjectId)> SeedSingleAxisWithProjectAsync(
        CapexTestServer server,
        int adminId)
    {
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var now = DateTimeOffset.UtcNow;
        var actor = new UserId(adminId);
        var program = ProjectProgram.Create("Solo", "SOLO", actor, now);
        database.Add(program);
        await database.SaveChangesAsync();
        var sourceAxis = ProjectAxis.Create(program.Id, "Only", "ONLY", actor, now);
        database.Add(sourceAxis);
        await database.SaveChangesAsync();
        var project = Project.Create(
            new ProjectItemValues(sourceAxis.Id, "Only project", ProjectStatus.Planning, RecordVisibility.Public),
            1,
            actor,
            now);
        database.Add(project);
        await database.SaveChangesAsync();
        return (program.Id, sourceAxis.Id, project.Id);
    }

    private sealed record ProblemPayload(string? Code);
}

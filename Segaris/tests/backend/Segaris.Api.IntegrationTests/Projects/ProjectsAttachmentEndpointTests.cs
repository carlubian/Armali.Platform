using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Projects;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Shared.Attachments;

namespace Segaris.Api.IntegrationTests.Projects;

public sealed class ProjectsAttachmentEndpointTests
{
    [Fact]
    public async Task Attachments_round_trip_through_upload_list_detail_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (_, _, project) = await CreateProjectTreeAsync(client, csrf);
        var baseRoute = $"/api/projects/projects/{project.Id}/attachments";
        var content = Encoding.UTF8.GetBytes("Project result notes");

        using var upload = await CapexApi.UploadAsync(
            client,
            baseRoute,
            "result.txt",
            "text/plain",
            content,
            csrf);
        var created = await upload.Content.ReadFromJsonAsync<ProjectAttachmentResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("result.txt", created!.FileName);

        var list = await client.GetFromJsonAsync<ProjectAttachmentResponse[]>(baseRoute, CancellationToken.None);
        var detail = await client.GetFromJsonAsync<ProjectResponse>(
            $"/api/projects/projects/{project.Id}",
            CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(list!).Id);
        Assert.Equal(created.Id, Assert.Single(detail!.Attachments).Id);

        var downloaded = await client.GetByteArrayAsync($"{baseRoute}/{created.Id}", CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(client, $"{baseRoute}/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<ProjectAttachmentResponse[]>(baseRoute, CancellationToken.None);
        Assert.Empty(empty!);
    }

    [Fact]
    public async Task Uploading_without_an_antiforgery_token_is_rejected()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (_, _, project) = await CreateProjectTreeAsync(client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/projects/projects/{project.Id}/attachments",
            "result.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task Uploading_a_rejected_file_returns_the_project_attachment_invalid_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (_, _, project) = await CreateProjectTreeAsync(client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/projects/projects/{project.Id}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var problem = await upload.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Equal("projects.attachment.invalid", problem!.Code);
    }

    [Fact]
    public async Task Attachment_routes_inherit_project_visibility()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        var (_, axis, _) = await CreateProjectTreeAsync(admin, adminCsrf);
        await server.CreateUserAsync("projects-attachment-owner", "OwnerPass123!");
        await server.CreateUserAsync("projects-attachment-collaborator", "CollaboratorPass123!");
        using var owner = await server.CreateAuthenticatedClientAsync("projects-attachment-owner", "OwnerPass123!");
        using var collaborator = await server.CreateAuthenticatedClientAsync("projects-attachment-collaborator", "CollaboratorPass123!");
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(owner);
        var collaboratorCsrf = await CapexTestServer.GetCsrfTokenAsync(collaborator);

        var privateProject = await CreateProjectAsync(
            owner,
            ownerCsrf,
            new CreateProjectRequest(axis.Id, "Private project", null, "Private"));
        var privateRoute = $"/api/projects/projects/{privateProject.Id}/attachments";

        using var list = await collaborator.GetAsync(privateRoute, CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            collaborator,
            privateRoute,
            "result.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            collaboratorCsrf);
        using var download = await collaborator.GetAsync($"{privateRoute}/1", CancellationToken.None);
        using var delete = await CapexApi.DeleteAsync(collaborator, $"{privateRoute}/1", collaboratorCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var problem = await list.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("projects.project.not_found", problem!.Code);

        var publicProject = await CreateProjectAsync(
            owner,
            ownerCsrf,
            new CreateProjectRequest(axis.Id, "Public project", null, "Public"));
        using var collaboratorUpload = await CapexApi.UploadAsync(
            collaborator,
            $"/api/projects/projects/{publicProject.Id}/attachments",
            "public-result.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("collaborative result"),
            collaboratorCsrf);

        Assert.Equal(HttpStatusCode.Created, collaboratorUpload.StatusCode);
    }

    [Fact]
    public async Task Missing_attachments_return_the_project_attachment_not_found_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (_, _, project) = await CreateProjectTreeAsync(client, csrf);
        var baseRoute = $"/api/projects/projects/{project.Id}/attachments";

        using var download = await client.GetAsync($"{baseRoute}/424242", CancellationToken.None);
        using var delete = await CapexApi.DeleteAsync(client, $"{baseRoute}/424242", csrf);
        var problem = await delete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal("projects.attachment.not_found", problem!.Code);
    }

    [Fact]
    public async Task Deleting_a_project_removes_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (_, _, project) = await CreateProjectTreeAsync(client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/projects/projects/{project.Id}/attachments",
            "result.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("Result"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/projects/projects/{project.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            var remaining = await attachments.ListByOwnerAsync(
                ProjectsAttachments.ProjectOwner(project.Id),
                CancellationToken.None);
            Assert.Empty(remaining);
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, ProjectsAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private static async Task<(ProgramResponse Program, AxisResponse Axis, ProjectResponse Project)> CreateProjectTreeAsync(
        HttpClient client,
        string csrf)
    {
        var program = await CreateProgramAsync(client, csrf, "Program", "PROG");
        var axis = await CreateAxisAsync(client, csrf, program.Id, "Axis", "AXIS");
        var project = await CreateProjectAsync(client, csrf, new CreateProjectRequest(axis.Id, "Project one", null, null));
        return (program, axis, project);
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

    private sealed record ProblemPayload(string? Code);
}

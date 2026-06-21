using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Processes;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Processes;

public sealed class ProcessAttachmentTests
{
    [Fact]
    public async Task Attachments_round_trip_through_upload_list_detail_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var process = await CreateProcessAsync(client, csrf);
        var route = $"/api/processes/{process.Id}/attachments";
        var content = Encoding.UTF8.GetBytes("Process documents");

        using var upload = await CapexApi.UploadAsync(
            client,
            route,
            "documents.txt",
            "text/plain",
            content,
            csrf);
        var created = await upload.Content.ReadFromJsonAsync<ProcessAttachmentResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("documents.txt", created!.FileName);

        var list = await client.GetFromJsonAsync<ProcessAttachmentResponse[]>(route, CancellationToken.None);
        var detail = await client.GetFromJsonAsync<ProcessResponse>(
            $"/api/processes/{process.Id}",
            CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(list!).Id);
        Assert.Equal(created.Id, Assert.Single(detail!.Attachments).Id);

        var downloaded = await client.GetByteArrayAsync($"{route}/{created.Id}", CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(client, $"{route}/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<ProcessAttachmentResponse[]>(route, CancellationToken.None);
        Assert.Empty(empty!);
    }

    [Fact]
    public async Task Uploading_without_an_antiforgery_token_is_rejected()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var process = await CreateProcessAsync(client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/processes/{process.Id}/attachments",
            "documents.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task Uploading_a_rejected_file_returns_the_process_attachment_invalid_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var process = await CreateProcessAsync(client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/processes/{process.Id}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var problem = await upload.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Equal("processes.attachment.invalid", problem!.Code);
    }

    [Fact]
    public async Task Attachment_routes_inherit_process_visibility()
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("process-attachment-owner", "OwnerPass123!");
        await server.CreateUserAsync("process-attachment-collaborator", "CollaboratorPass123!");
        var ownerId = await server.GetUserIdAsync("process-attachment-owner");
        var privateProcessId = await ProcessTestData.SeedProcessAsync(
            server.Services,
            ownerId,
            name: "Private process",
            visibility: RecordVisibility.Private);
        var publicProcessId = await ProcessTestData.SeedProcessAsync(
            server.Services,
            ownerId,
            name: "Public process",
            visibility: RecordVisibility.Public);
        using var collaborator = await server.CreateAuthenticatedClientAsync(
            "process-attachment-collaborator",
            "CollaboratorPass123!");
        var collaboratorCsrf = await CapexTestServer.GetCsrfTokenAsync(collaborator);
        var privateRoute = $"/api/processes/{privateProcessId}/attachments";

        using var list = await collaborator.GetAsync(privateRoute, CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            collaborator,
            privateRoute,
            "documents.txt",
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
        Assert.Equal("processes.process.not_found", problem!.Code);

        using var collaboratorUpload = await CapexApi.UploadAsync(
            collaborator,
            $"/api/processes/{publicProcessId}/attachments",
            "public-documents.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("collaborative documents"),
            collaboratorCsrf);

        Assert.Equal(HttpStatusCode.Created, collaboratorUpload.StatusCode);
    }

    [Fact]
    public async Task Missing_attachments_return_the_process_attachment_not_found_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var process = await CreateProcessAsync(client, csrf);
        var route = $"/api/processes/{process.Id}/attachments";

        using var download = await client.GetAsync($"{route}/424242", CancellationToken.None);
        using var delete = await CapexApi.DeleteAsync(client, $"{route}/424242", csrf);
        var problem = await delete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal("processes.attachment.not_found", problem!.Code);
    }

    [Fact]
    public async Task Deleting_a_process_removes_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var process = await CreateProcessAsync(client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/processes/{process.Id}/attachments",
            "documents.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("Documents"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/processes/{process.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            var remaining = await attachments.ListByOwnerAsync(
                ProcessesAttachments.ProcessOwner(process.Id),
                CancellationToken.None);
            Assert.Empty(remaining);
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, ProcessesAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private static async Task<ProcessResponse> CreateProcessAsync(HttpClient client, string csrf)
    {
        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/processes",
            ProcessRequestBuilder.Default().BuildCreate(),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProcessResponse>(CancellationToken.None))!;
    }

    private sealed record ProblemPayload(string? Code);
}

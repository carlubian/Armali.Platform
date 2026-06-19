using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Maintenance;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Maintenance;

public sealed class MaintenanceTaskAttachmentTests
{
    [Fact]
    public async Task Attachments_round_trip_through_upload_list_detail_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var taskId = await MaintenanceTestData.SeedTaskAsync(server.Services, founderId, title: "Attach manual");
        var content = Encoding.UTF8.GetBytes("Maintenance manual");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/maintenance/tasks/{taskId}/attachments",
            "manual.txt",
            "text/plain",
            content,
            csrf);
        var created = await upload.Content.ReadFromJsonAsync<MaintenanceTaskAttachmentResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("manual.txt", created!.FileName);

        var list = await client.GetFromJsonAsync<MaintenanceTaskAttachmentResponse[]>(
            $"/api/maintenance/tasks/{taskId}/attachments",
            CancellationToken.None);
        var detail = await client.GetFromJsonAsync<MaintenanceTaskResponse>(
            $"/api/maintenance/tasks/{taskId}",
            CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(list!).Id);
        Assert.Equal(created.Id, Assert.Single(detail!.Attachments).Id);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/maintenance/tasks/{taskId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/maintenance/tasks/{taskId}/attachments/{created.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<MaintenanceTaskAttachmentResponse[]>(
            $"/api/maintenance/tasks/{taskId}/attachments",
            CancellationToken.None);
        Assert.Empty(empty!);
    }

    [Fact]
    public async Task Uploading_without_an_antiforgery_token_is_rejected()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var taskId = await MaintenanceTestData.SeedTaskAsync(server.Services, founderId);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/maintenance/tasks/{taskId}/attachments",
            "manual.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task Uploading_a_rejected_file_returns_the_maintenance_attachment_invalid_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var taskId = await MaintenanceTestData.SeedTaskAsync(server.Services, founderId);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/maintenance/tasks/{taskId}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var problem = await upload.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Equal("maintenance.attachment.invalid", problem!.Code);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_task()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var taskId = await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Private attachment task",
            visibility: RecordVisibility.Private);

        await server.CreateUserAsync("maintenance-attacher", "MaintenanceAttacher123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "maintenance-attacher", "MaintenanceAttacher123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync(
            $"/api/maintenance/tasks/{taskId}/attachments",
            CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member,
            $"/api/maintenance/tasks/{taskId}/attachments",
            "manual.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            csrf);
        using var download = await member.GetAsync(
            $"/api/maintenance/tasks/{taskId}/attachments/1",
            CancellationToken.None);
        using var delete = await CapexApi.DeleteAsync(
            member,
            $"/api/maintenance/tasks/{taskId}/attachments/1",
            csrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var problem = await list.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("maintenance.task.not_found", problem!.Code);
    }

    [Fact]
    public async Task Missing_attachments_return_the_maintenance_attachment_not_found_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var taskId = await MaintenanceTestData.SeedTaskAsync(server.Services, founderId);

        using var download = await client.GetAsync(
            $"/api/maintenance/tasks/{taskId}/attachments/424242",
            CancellationToken.None);
        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/maintenance/tasks/{taskId}/attachments/424242",
            csrf);
        var problem = await delete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal("maintenance.attachment.not_found", problem!.Code);
    }

    [Fact]
    public async Task Deleting_a_task_removes_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var taskId = await MaintenanceTestData.SeedTaskAsync(server.Services, founderId);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/maintenance/tasks/{taskId}/attachments",
            "manual.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("Manual"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/maintenance/tasks/{taskId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            var remaining = await attachments.ListByOwnerAsync(
                MaintenanceAttachments.TaskOwner(taskId),
                CancellationToken.None);
            Assert.Empty(remaining);
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, MaintenanceAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private sealed record ProblemPayload(string? Code);
}

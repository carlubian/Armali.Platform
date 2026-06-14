using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Capex;
using Segaris.Shared.Attachments;

namespace Segaris.Api.IntegrationTests.Capex;

public sealed class CapexAttachmentTests
{
    private const string MemberName = "attacher";
    private const string MemberPassword = "AttacherPass123!";

    [Fact]
    public async Task Attachments_round_trip_through_upload_list_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(server, client, csrf, builder => builder);
        var content = Encoding.UTF8.GetBytes("Receipt total: 42");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/capex/entries/{entryId}/attachments",
            "receipt.txt",
            "text/plain",
            content,
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var created = await upload.Content.ReadFromJsonAsync<AttachmentPayload>(CancellationToken.None);
        Assert.Equal("receipt.txt", created!.FileName);

        var list = await client.GetFromJsonAsync<AttachmentPayload[]>(
            $"/api/capex/entries/{entryId}/attachments",
            CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(list!).Id);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/capex/entries/{entryId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/capex/entries/{entryId}/attachments/{created.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<AttachmentPayload[]>(
            $"/api/capex/entries/{entryId}/attachments",
            CancellationToken.None);
        Assert.Empty(empty!);
    }

    [Fact]
    public async Task Uploading_without_an_antiforgery_token_is_rejected()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(server, client, csrf, builder => builder);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/capex/entries/{entryId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task Uploading_a_rejected_file_returns_the_capex_attachment_invalid_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(server, client, csrf, builder => builder);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/capex/entries/{entryId}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var problem = await upload.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Equal("capex.attachment.invalid", problem!.Code);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_entry()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(
            server,
            founder,
            founderCsrf,
            builder => builder.WithVisibility("Private"));

        await server.CreateUserAsync(MemberName, MemberPassword);
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, MemberName, MemberPassword);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync($"/api/capex/entries/{entryId}/attachments", CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member,
            $"/api/capex/entries/{entryId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        var problem = await list.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("capex.entry.not_found", problem!.Code);
    }

    [Fact]
    public async Task A_missing_attachment_returns_the_capex_attachment_not_found_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(server, client, csrf, builder => builder);

        using var download = await client.GetAsync(
            $"/api/capex/entries/{entryId}/attachments/424242",
            CancellationToken.None);
        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/capex/entries/{entryId}/attachments/424242",
            csrf);

        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var problem = await delete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("capex.attachment.not_found", problem!.Code);
    }

    [Fact]
    public async Task Deleting_an_entry_removes_its_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var entryId = await CapexEntryMutationTests.CreateEntryAsync(server, client, csrf, builder => builder);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/capex/entries/{entryId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("Receipt"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/capex/entries/{entryId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // The attachment metadata is gone through the platform service...
        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            var remaining = await attachments.ListByOwnerAsync(CapexAttachments.Owner(entryId), CancellationToken.None);
            Assert.Empty(remaining);
        }

        // ...and the physical files have been removed from the Capex module store.
        var moduleDirectory = Path.Combine(server.AttachmentsPath, CapexAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private sealed record AttachmentPayload(string Id, string FileName, string ContentType, long Size);

    private sealed record ProblemPayload(string? Code);
}

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryItemAttachmentTests
{
    private const string MemberName = "inventory-attacher";
    private const string MemberPassword = "InventoryAttacher123!";

    [Fact]
    public async Task Attachments_round_trip_through_upload_list_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var itemId = await InventoryItemMutationTests.CreateItemAsync(server, client, csrf, builder => builder.WithName("Attachment item"));
        var content = Encoding.UTF8.GetBytes("Inventory receipt");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/inventory/items/{itemId}/attachments",
            "receipt.txt",
            "text/plain",
            content,
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var created = await upload.Content.ReadFromJsonAsync<AttachmentPayload>(CancellationToken.None);
        Assert.Equal("receipt.txt", created!.FileName);

        var list = await client.GetFromJsonAsync<AttachmentPayload[]>(
            $"/api/inventory/items/{itemId}/attachments",
            CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(list!).Id);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/inventory/items/{itemId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/inventory/items/{itemId}/attachments/{created.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<AttachmentPayload[]>(
            $"/api/inventory/items/{itemId}/attachments",
            CancellationToken.None);
        Assert.Empty(empty!);
    }

    [Fact]
    public async Task Uploading_rejected_file_returns_inventory_attachment_invalid_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var itemId = await InventoryItemMutationTests.CreateItemAsync(server, client, csrf, builder => builder);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/inventory/items/{itemId}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var problem = await upload.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Equal("inventory.attachment.invalid", problem!.Code);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_item()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var privateItemId = await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            visibility: RecordVisibility.Private);

        await server.CreateUserAsync(MemberName, MemberPassword);
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, MemberName, MemberPassword);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync($"/api/inventory/items/{privateItemId}/attachments", CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member,
            $"/api/inventory/items/{privateItemId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        var problem = await list.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("inventory.item.not_found", problem!.Code);
    }

    [Fact]
    public async Task Deleting_an_item_removes_its_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var itemId = await InventoryItemMutationTests.CreateItemAsync(server, client, csrf, builder => builder);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/inventory/items/{itemId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("Receipt"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/inventory/items/{itemId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            var remaining = await attachments.ListByOwnerAsync(InventoryAttachments.ItemOwner(itemId), CancellationToken.None);
            Assert.Empty(remaining);
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, InventoryAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private sealed record AttachmentPayload(string Id, string FileName, string ContentType, long Size);

    private sealed record ProblemPayload(string? Code);
}

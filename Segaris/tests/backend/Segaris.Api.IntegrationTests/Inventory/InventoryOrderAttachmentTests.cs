using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryOrderAttachmentTests
{
    [Fact]
    public async Task Attachments_round_trip_through_upload_list_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId);
        var orderId = await InventoryOrderMutationTests.CreateOrderAsync(server, client, csrf, itemId, builder => builder);
        var content = Encoding.UTF8.GetBytes("Inventory order receipt");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/inventory/orders/{orderId}/attachments",
            "receipt.txt",
            "text/plain",
            content,
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var created = await upload.Content.ReadFromJsonAsync<AttachmentPayload>(CancellationToken.None);

        var list = await client.GetFromJsonAsync<AttachmentPayload[]>(
            $"/api/inventory/orders/{orderId}/attachments",
            CancellationToken.None);
        Assert.Equal(created!.Id, Assert.Single(list!).Id);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/inventory/orders/{orderId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/inventory/orders/{orderId}/attachments/{created.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_order()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId);
        var privateOrderId = await InventoryTestData.SeedOrderAsync(
            server.Services,
            founderId,
            itemId,
            visibility: RecordVisibility.Private);

        await server.CreateUserAsync("inventory-order-attacher", "OrderAttacher123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "inventory-order-attacher", "OrderAttacher123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync($"/api/inventory/orders/{privateOrderId}/attachments", CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member,
            $"/api/inventory/orders/{privateOrderId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            csrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        var problem = await list.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("inventory.order.not_found", problem!.Code);
    }

    [Fact]
    public async Task Deleting_an_order_removes_its_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId);
        var orderId = await InventoryOrderMutationTests.CreateOrderAsync(server, client, csrf, itemId, builder => builder);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/inventory/orders/{orderId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("Receipt"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/inventory/orders/{orderId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            var remaining = await attachments.ListByOwnerAsync(InventoryAttachments.OrderOwner(orderId), CancellationToken.None);
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

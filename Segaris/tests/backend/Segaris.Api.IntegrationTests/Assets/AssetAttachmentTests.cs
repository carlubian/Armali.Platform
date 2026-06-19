using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Assets;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Assets;

public sealed class AssetAttachmentTests
{
    [Fact]
    public async Task Attachments_round_trip_through_upload_list_detail_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Attachment asset");
        var content = PngBytes(1);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/assets/items/{assetId}/attachments",
            "front.png",
            "image/png",
            content,
            csrf);
        var created = await upload.Content.ReadFromJsonAsync<AssetAttachmentResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("front.png", created!.FileName);
        Assert.False(created.IsPrimary);

        var list = await client.GetFromJsonAsync<AssetAttachmentResponse[]>(
            $"/api/assets/items/{assetId}/attachments",
            CancellationToken.None);
        var detail = await GetAssetAsync(client, assetId);
        Assert.Equal(created.Id, Assert.Single(list!).Id);
        Assert.Equal(created.Id, Assert.Single(detail.Attachments).Id);
        Assert.Equal("image", detail.Thumbnail.Source);
        Assert.Equal(created.Id, detail.Thumbnail.AttachmentId);
        Assert.Equal($"/api/assets/items/{assetId}/attachments/{created.Id}", detail.Thumbnail.Url);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/assets/items/{assetId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/assets/items/{assetId}/attachments/{created.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<AssetAttachmentResponse[]>(
            $"/api/assets/items/{assetId}/attachments",
            CancellationToken.None);
        var afterDelete = await GetAssetAsync(client, assetId);
        Assert.Empty(empty!);
        Assert.Equal("placeholder", afterDelete.Thumbnail.Source);
        Assert.Null(afterDelete.Thumbnail.AttachmentId);
        Assert.Null(afterDelete.Thumbnail.Url);
    }

    [Fact]
    public async Task Primary_image_selection_drives_the_thumbnail_and_falls_back_when_deleted()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Primary asset");

        var first = await UploadImageAsync(client, assetId, "first.png", PngBytes(1), csrf);
        var second = await UploadImageAsync(client, assetId, "second.png", PngBytes(2), csrf);

        var beforePrimary = await GetAssetAsync(client, assetId);
        Assert.Equal("image", beforePrimary.Thumbnail.Source);
        Assert.Equal(first.Id, beforePrimary.Thumbnail.AttachmentId);
        Assert.All(beforePrimary.Attachments, attachment => Assert.False(attachment.IsPrimary));

        using var setPrimary = await CapexApi.PutAsync(
            client,
            $"/api/assets/items/{assetId}/attachments/{second.Id}/primary",
            csrf);
        var marked = await setPrimary.Content.ReadFromJsonAsync<AssetAttachmentResponse>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, setPrimary.StatusCode);
        Assert.Equal(second.Id, marked!.Id);
        Assert.True(marked.IsPrimary);

        var afterPrimary = await GetAssetAsync(client, assetId);
        var summary = await GetSummaryAsync(client, assetId);
        Assert.Equal("primary", afterPrimary.Thumbnail.Source);
        Assert.Equal(second.Id, afterPrimary.Thumbnail.AttachmentId);
        Assert.Equal("primary", summary.Thumbnail.Source);
        Assert.Equal(second.Id, summary.Thumbnail.AttachmentId);
        Assert.True(afterPrimary.Attachments.Single(attachment => attachment.Id == second.Id).IsPrimary);
        Assert.False(afterPrimary.Attachments.Single(attachment => attachment.Id == first.Id).IsPrimary);

        using var deletePrimary = await CapexApi.DeleteAsync(
            client,
            $"/api/assets/items/{assetId}/attachments/{second.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, deletePrimary.StatusCode);

        var afterFallback = await GetAssetAsync(client, assetId);
        Assert.Equal("image", afterFallback.Thumbnail.Source);
        Assert.Equal(first.Id, afterFallback.Thumbnail.AttachmentId);
        Assert.False(Assert.Single(afterFallback.Attachments).IsPrimary);
    }

    [Fact]
    public async Task Non_image_attachments_cannot_be_primary_and_never_drive_the_thumbnail()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Manual asset");

        var document = await UploadImageAsync(
            client,
            assetId,
            "manual.txt",
            Encoding.UTF8.GetBytes("Manual"),
            csrf,
            contentType: "text/plain");

        using var setPrimary = await CapexApi.PutAsync(
            client,
            $"/api/assets/items/{assetId}/attachments/{document.Id}/primary",
            csrf);
        var problem = await setPrimary.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var detail = await GetAssetAsync(client, assetId);

        Assert.Equal(HttpStatusCode.BadRequest, setPrimary.StatusCode);
        Assert.Equal("assets.attachment.primary_invalid", problem!.Code);
        Assert.Equal("placeholder", detail.Thumbnail.Source);
        Assert.False(Assert.Single(detail.Attachments).IsPrimary);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_asset()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        await server.CreateUserAsync("asset-attacher", "AssetAttacher123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "asset-attacher", "AssetAttacher123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync(
            $"/api/assets/items/{assetId}/attachments",
            CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member,
            $"/api/assets/items/{assetId}/attachments",
            "front.png",
            "image/png",
            PngBytes(1),
            memberCsrf);
        using var download = await member.GetAsync(
            $"/api/assets/items/{assetId}/attachments/1",
            CancellationToken.None);
        using var setPrimary = await CapexApi.PutAsync(
            member,
            $"/api/assets/items/{assetId}/attachments/1/primary",
            memberCsrf);
        using var delete = await CapexApi.DeleteAsync(
            member,
            $"/api/assets/items/{assetId}/attachments/1",
            memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, setPrimary.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Missing_and_invalid_attachments_return_asset_problem_codes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(server.Services, founderId);

        using var missingDelete = await CapexApi.DeleteAsync(
            client,
            $"/api/assets/items/{assetId}/attachments/424242",
            csrf);
        using var missingPrimary = await CapexApi.PutAsync(
            client,
            $"/api/assets/items/{assetId}/attachments/424242/primary",
            csrf);
        using var invalid = await CapexApi.UploadAsync(
            client,
            $"/api/assets/items/{assetId}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var missingDeleteProblem = await missingDelete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var missingPrimaryProblem = await missingPrimary.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidProblem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
        Assert.Equal("assets.attachment.not_found", missingDeleteProblem!.Code);
        Assert.Equal(HttpStatusCode.NotFound, missingPrimary.StatusCode);
        Assert.Equal("assets.attachment.not_found", missingPrimaryProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("assets.attachment.invalid", invalidProblem!.Code);
    }

    [Fact]
    public async Task Deleting_an_asset_removes_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var assetId = await AssetsTestData.SeedAssetAsync(server.Services, founderId, name: "Disposable");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/assets/items/{assetId}/attachments",
            "front.png",
            "image/png",
            PngBytes(1),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/assets/items/{assetId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            Assert.Empty(await attachments.ListByOwnerAsync(
                AssetsAttachments.AssetOwner(assetId),
                CancellationToken.None));
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, AssetsAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private static async Task<AssetAttachmentResponse> UploadImageAsync(
        HttpClient client,
        int assetId,
        string fileName,
        byte[] content,
        string? csrf,
        string contentType = "image/png")
    {
        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/assets/items/{assetId}/attachments",
            fileName,
            contentType,
            content,
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var created = await upload.Content.ReadFromJsonAsync<AssetAttachmentResponse>(CancellationToken.None);
        return created!;
    }

    private static async Task<AssetResponse> GetAssetAsync(HttpClient client, int assetId)
    {
        var asset = await client.GetFromJsonAsync<AssetResponse>(
            $"/api/assets/items/{assetId}",
            CancellationToken.None);
        Assert.NotNull(asset);
        return asset;
    }

    private static async Task<AssetSummaryResponse> GetSummaryAsync(HttpClient client, int assetId)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<AssetSummaryResponse>>(
            $"/api/assets/items?search={Uri.EscapeDataString("Primary")}",
            CancellationToken.None);
        Assert.NotNull(page);
        return page.Items.Single(item => item.Id == assetId);
    }

    private static byte[] PngBytes(byte marker) =>
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, marker];

    private sealed record ProblemPayload(string? Code);
}

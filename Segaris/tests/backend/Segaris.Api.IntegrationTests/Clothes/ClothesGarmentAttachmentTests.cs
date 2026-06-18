using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Clothes;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Clothes;

public sealed class ClothesGarmentAttachmentTests
{
    [Fact]
    public async Task Attachments_round_trip_through_upload_list_detail_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var garmentId = await ClothesTestData.SeedGarmentAsync(server.Services, founderId, name: "Attachment coat");
        var content = PngBytes(1);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments",
            "front.png",
            "image/png",
            content,
            csrf);
        var created = await upload.Content.ReadFromJsonAsync<ClothesAttachmentResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("front.png", created!.FileName);
        Assert.False(created.IsPrimary);

        var list = await client.GetFromJsonAsync<ClothesAttachmentResponse[]>(
            $"/api/clothes/garments/{garmentId}/attachments",
            CancellationToken.None);
        var detail = await GetGarmentAsync(client, garmentId);
        Assert.Equal(created.Id, Assert.Single(list!).Id);
        Assert.Equal(created.Id, Assert.Single(detail.Attachments).Id);
        Assert.Equal("firstImage", detail.Thumbnail.Source);
        Assert.Equal(created.Id, detail.Thumbnail.AttachmentId);
        Assert.Equal($"/api/clothes/garments/{garmentId}/attachments/{created.Id}", detail.Thumbnail.Url);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/clothes/garments/{garmentId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments/{created.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<ClothesAttachmentResponse[]>(
            $"/api/clothes/garments/{garmentId}/attachments",
            CancellationToken.None);
        var afterDelete = await GetGarmentAsync(client, garmentId);
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
        var garmentId = await ClothesTestData.SeedGarmentAsync(server.Services, founderId, name: "Primary coat");

        var first = await UploadImageAsync(client, garmentId, "first.png", PngBytes(1), csrf);
        var second = await UploadImageAsync(client, garmentId, "second.png", PngBytes(2), csrf);

        var beforePrimary = await GetGarmentAsync(client, garmentId);
        Assert.Equal("firstImage", beforePrimary.Thumbnail.Source);
        Assert.Equal(first.Id, beforePrimary.Thumbnail.AttachmentId);
        Assert.All(beforePrimary.Attachments, attachment => Assert.False(attachment.IsPrimary));

        using var setPrimary = await CapexApi.PutAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments/{second.Id}/primary",
            csrf);
        var marked = await setPrimary.Content.ReadFromJsonAsync<ClothesAttachmentResponse>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, setPrimary.StatusCode);
        Assert.Equal(second.Id, marked!.Id);
        Assert.True(marked.IsPrimary);

        var afterPrimary = await GetGarmentAsync(client, garmentId);
        var summary = await GetSummaryAsync(client, garmentId);
        Assert.Equal("primary", afterPrimary.Thumbnail.Source);
        Assert.Equal(second.Id, afterPrimary.Thumbnail.AttachmentId);
        Assert.Equal("primary", summary.Thumbnail.Source);
        Assert.Equal(second.Id, summary.Thumbnail.AttachmentId);
        Assert.True(afterPrimary.Attachments.Single(attachment => attachment.Id == second.Id).IsPrimary);
        Assert.False(afterPrimary.Attachments.Single(attachment => attachment.Id == first.Id).IsPrimary);

        using var deletePrimary = await CapexApi.DeleteAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments/{second.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, deletePrimary.StatusCode);

        var afterFallback = await GetGarmentAsync(client, garmentId);
        Assert.Equal("firstImage", afterFallback.Thumbnail.Source);
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
        var garmentId = await ClothesTestData.SeedGarmentAsync(server.Services, founderId, name: "Receipt coat");

        var document = await UploadImageAsync(
            client,
            garmentId,
            "care.txt",
            Encoding.UTF8.GetBytes("Hand wash only"),
            csrf,
            contentType: "text/plain");

        using var setPrimary = await CapexApi.PutAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments/{document.Id}/primary",
            csrf);
        var problem = await setPrimary.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var detail = await GetGarmentAsync(client, garmentId);

        Assert.Equal(HttpStatusCode.BadRequest, setPrimary.StatusCode);
        Assert.Equal("clothes.attachment.primary_invalid", problem!.Code);
        Assert.Equal("placeholder", detail.Thumbnail.Source);
        Assert.False(Assert.Single(detail.Attachments).IsPrimary);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_garment()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var garmentId = await ClothesTestData.SeedGarmentAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        await server.CreateUserAsync("clothes-attacher", "ClothesAttacher123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "clothes-attacher", "ClothesAttacher123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync(
            $"/api/clothes/garments/{garmentId}/attachments",
            CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member,
            $"/api/clothes/garments/{garmentId}/attachments",
            "front.png",
            "image/png",
            PngBytes(1),
            memberCsrf);
        using var download = await member.GetAsync(
            $"/api/clothes/garments/{garmentId}/attachments/1",
            CancellationToken.None);
        using var setPrimary = await CapexApi.PutAsync(
            member,
            $"/api/clothes/garments/{garmentId}/attachments/1/primary",
            memberCsrf);
        using var delete = await CapexApi.DeleteAsync(
            member,
            $"/api/clothes/garments/{garmentId}/attachments/1",
            memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, setPrimary.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Missing_and_invalid_attachments_return_clothes_problem_codes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var garmentId = await ClothesTestData.SeedGarmentAsync(server.Services, founderId);

        using var missingDelete = await CapexApi.DeleteAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments/424242",
            csrf);
        using var missingPrimary = await CapexApi.PutAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments/424242/primary",
            csrf);
        using var invalid = await CapexApi.UploadAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var missingDeleteProblem = await missingDelete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var missingPrimaryProblem = await missingPrimary.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidProblem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
        Assert.Equal("clothes.attachment.not_found", missingDeleteProblem!.Code);
        Assert.Equal(HttpStatusCode.NotFound, missingPrimary.StatusCode);
        Assert.Equal("clothes.attachment.not_found", missingPrimaryProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("clothes.attachment.invalid", invalidProblem!.Code);
    }

    [Fact]
    public async Task Deleting_a_garment_removes_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var garmentId = await ClothesTestData.SeedGarmentAsync(server.Services, founderId, name: "Disposable");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments",
            "front.png",
            "image/png",
            PngBytes(1),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/clothes/garments/{garmentId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            Assert.Empty(await attachments.ListByOwnerAsync(
                ClothesAttachments.GarmentOwner(garmentId),
                CancellationToken.None));
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, ClothesAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private static async Task<ClothesAttachmentResponse> UploadImageAsync(
        HttpClient client,
        int garmentId,
        string fileName,
        byte[] content,
        string? csrf,
        string contentType = "image/png")
    {
        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/clothes/garments/{garmentId}/attachments",
            fileName,
            contentType,
            content,
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var created = await upload.Content.ReadFromJsonAsync<ClothesAttachmentResponse>(CancellationToken.None);
        return created!;
    }

    private static async Task<ClothesGarmentResponse> GetGarmentAsync(HttpClient client, int garmentId)
    {
        var garment = await client.GetFromJsonAsync<ClothesGarmentResponse>(
            $"/api/clothes/garments/{garmentId}",
            CancellationToken.None);
        Assert.NotNull(garment);
        return garment;
    }

    private static async Task<ClothesGarmentSummaryResponse> GetSummaryAsync(HttpClient client, int garmentId)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<ClothesGarmentSummaryResponse>>(
            $"/api/clothes/garments?search={Uri.EscapeDataString("coat")}",
            CancellationToken.None);
        Assert.NotNull(page);
        return page.Items.Single(item => item.Id == garmentId);
    }

    private static byte[] PngBytes(byte marker) =>
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, marker];

    private sealed record ProblemPayload(string? Code);
}

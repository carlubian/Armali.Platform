using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Destinations;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Destinations;

public sealed class DestinationAttachmentEndpointTests
{
    [Fact]
    public async Task Attachments_round_trip_through_upload_list_detail_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Attachment destination");
        var content = PngBytes(1);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/destinations/{destinationId}/attachments",
            "front.png",
            "image/png",
            content,
            csrf);
        var created = await upload.Content.ReadFromJsonAsync<DestinationAttachmentResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("front.png", created!.FileName);
        Assert.False(created.IsPrimary);

        var list = await client.GetFromJsonAsync<DestinationAttachmentResponse[]>(
            $"/api/destinations/{destinationId}/attachments",
            CancellationToken.None);
        var detail = await GetDestinationAsync(client, destinationId);
        Assert.Equal(created.Id, Assert.Single(list!).Id);
        Assert.Equal(created.Id, Assert.Single(detail.Attachments).Id);
        Assert.Equal("image", detail.Thumbnail.Source);
        Assert.Equal(created.Id, detail.Thumbnail.AttachmentId);
        Assert.Equal($"/api/destinations/{destinationId}/attachments/{created.Id}", detail.Thumbnail.Url);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/destinations/{destinationId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/destinations/{destinationId}/attachments/{created.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<DestinationAttachmentResponse[]>(
            $"/api/destinations/{destinationId}/attachments",
            CancellationToken.None);
        var afterDelete = await GetDestinationAsync(client, destinationId);
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
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Primary destination");

        var first = await UploadAttachmentAsync(client, destinationId, "first.png", PngBytes(1), csrf);
        var second = await UploadAttachmentAsync(client, destinationId, "second.png", PngBytes(2), csrf);

        var beforePrimary = await GetDestinationAsync(client, destinationId);
        Assert.Equal("image", beforePrimary.Thumbnail.Source);
        Assert.Equal(first.Id, beforePrimary.Thumbnail.AttachmentId);
        Assert.All(beforePrimary.Attachments, attachment => Assert.False(attachment.IsPrimary));

        using var setPrimary = await CapexApi.PutAsync(
            client,
            $"/api/destinations/{destinationId}/attachments/{second.Id}/primary",
            csrf);
        var marked = await setPrimary.Content.ReadFromJsonAsync<DestinationAttachmentResponse>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, setPrimary.StatusCode);
        Assert.Equal(second.Id, marked!.Id);
        Assert.True(marked.IsPrimary);

        var afterPrimary = await GetDestinationAsync(client, destinationId);
        var summary = await GetSummaryAsync(client, destinationId);
        Assert.Equal("primary", afterPrimary.Thumbnail.Source);
        Assert.Equal(second.Id, afterPrimary.Thumbnail.AttachmentId);
        Assert.Equal("primary", summary.Thumbnail.Source);
        Assert.Equal(second.Id, summary.Thumbnail.AttachmentId);
        Assert.True(afterPrimary.Attachments.Single(attachment => attachment.Id == second.Id).IsPrimary);
        Assert.False(afterPrimary.Attachments.Single(attachment => attachment.Id == first.Id).IsPrimary);

        using var deletePrimary = await CapexApi.DeleteAsync(
            client,
            $"/api/destinations/{destinationId}/attachments/{second.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, deletePrimary.StatusCode);

        var afterFallback = await GetDestinationAsync(client, destinationId);
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
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Manual destination");

        var document = await UploadAttachmentAsync(
            client,
            destinationId,
            "manual.txt",
            Encoding.UTF8.GetBytes("Manual"),
            csrf,
            contentType: "text/plain");

        using var setPrimary = await CapexApi.PutAsync(
            client,
            $"/api/destinations/{destinationId}/attachments/{document.Id}/primary",
            csrf);
        var problem = await setPrimary.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var detail = await GetDestinationAsync(client, destinationId);

        Assert.Equal(HttpStatusCode.BadRequest, setPrimary.StatusCode);
        Assert.Equal("destinations.attachment.primary_invalid", problem!.Code);
        Assert.Equal("placeholder", detail.Thumbnail.Source);
        Assert.False(Assert.Single(detail.Attachments).IsPrimary);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_destination()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        await server.CreateUserAsync("destination-attacher", "DestinationAttacher123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "destination-attacher", "DestinationAttacher123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync(
            $"/api/destinations/{destinationId}/attachments",
            CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member,
            $"/api/destinations/{destinationId}/attachments",
            "front.png",
            "image/png",
            PngBytes(1),
            memberCsrf);
        using var download = await member.GetAsync(
            $"/api/destinations/{destinationId}/attachments/1",
            CancellationToken.None);
        using var setPrimary = await CapexApi.PutAsync(
            member,
            $"/api/destinations/{destinationId}/attachments/1/primary",
            memberCsrf);
        using var delete = await CapexApi.DeleteAsync(
            member,
            $"/api/destinations/{destinationId}/attachments/1",
            memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, setPrimary.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Missing_and_invalid_attachments_return_destination_problem_codes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId);

        using var missingDelete = await CapexApi.DeleteAsync(
            client,
            $"/api/destinations/{destinationId}/attachments/424242",
            csrf);
        using var missingPrimary = await CapexApi.PutAsync(
            client,
            $"/api/destinations/{destinationId}/attachments/424242/primary",
            csrf);
        using var invalid = await CapexApi.UploadAsync(
            client,
            $"/api/destinations/{destinationId}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var missingDeleteProblem = await missingDelete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var missingPrimaryProblem = await missingPrimary.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidProblem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
        Assert.Equal("destinations.attachment.not_found", missingDeleteProblem!.Code);
        Assert.Equal(HttpStatusCode.NotFound, missingPrimary.StatusCode);
        Assert.Equal("destinations.attachment.not_found", missingPrimaryProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("destinations.attachment.invalid", invalidProblem!.Code);
    }

    [Fact]
    public async Task Deleting_a_destination_removes_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Disposable");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/destinations/{destinationId}/attachments",
            "front.png",
            "image/png",
            PngBytes(1),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/destinations/{destinationId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            Assert.Empty(await attachments.ListByOwnerAsync(
                DestinationsAttachments.DestinationOwner(destinationId),
                CancellationToken.None));
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, DestinationsAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private static async Task<DestinationAttachmentResponse> UploadAttachmentAsync(
        HttpClient client,
        int destinationId,
        string fileName,
        byte[] content,
        string? csrf,
        string contentType = "image/png")
    {
        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/destinations/{destinationId}/attachments",
            fileName,
            contentType,
            content,
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var created = await upload.Content.ReadFromJsonAsync<DestinationAttachmentResponse>(CancellationToken.None);
        return created!;
    }

    private static async Task<DestinationResponse> GetDestinationAsync(HttpClient client, int destinationId)
    {
        var destination = await client.GetFromJsonAsync<DestinationResponse>(
            $"/api/destinations/{destinationId}",
            CancellationToken.None);
        Assert.NotNull(destination);
        return destination;
    }

    private static async Task<DestinationSummaryResponse> GetSummaryAsync(HttpClient client, int destinationId)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<DestinationSummaryResponse>>(
            $"/api/destinations?search={Uri.EscapeDataString("Primary")}",
            CancellationToken.None);
        Assert.NotNull(page);
        return page.Items.Single(item => item.Id == destinationId);
    }

    private static byte[] PngBytes(byte marker) =>
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, marker];

    private sealed record ProblemPayload(string? Code);
}

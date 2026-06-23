using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Health;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Health;

public sealed class HealthMedicineAttachmentTests
{
    [Fact]
    public async Task Attachments_round_trip_through_upload_list_detail_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var medicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "Attachment pill");
        var content = PngBytes(1);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments",
            "front.png",
            "image/png",
            content,
            csrf);
        var created = await upload.Content.ReadFromJsonAsync<MedicineAttachmentResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("front.png", created!.FileName);
        Assert.False(created.IsPrimary);

        var list = await client.GetFromJsonAsync<MedicineAttachmentResponse[]>(
            $"/api/health/medicines/{medicineId}/attachments",
            CancellationToken.None);
        var detail = await GetMedicineAsync(client, medicineId);
        Assert.Equal(created.Id, Assert.Single(list!).Id);
        Assert.Equal(created.Id, Assert.Single(detail.Attachments).Id);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/health/medicines/{medicineId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments/{created.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<MedicineAttachmentResponse[]>(
            $"/api/health/medicines/{medicineId}/attachments",
            CancellationToken.None);
        Assert.Empty(empty!);
    }

    [Fact]
    public async Task Primary_image_selection_drives_the_thumbnail_and_falls_back_when_deleted()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var medicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "Primary pill");

        var first = await UploadImageAsync(client, medicineId, "first.png", PngBytes(1), csrf);
        var second = await UploadImageAsync(client, medicineId, "second.png", PngBytes(2), csrf);

        var beforePrimary = await GetMedicineAsync(client, medicineId);
        Assert.All(beforePrimary.Attachments, attachment => Assert.False(attachment.IsPrimary));

        var beforeSummary = await GetSummaryAsync(client, medicineId);
        Assert.Equal("firstImage", beforeSummary.Thumbnail.Source);
        Assert.Equal(first.Id, beforeSummary.Thumbnail.AttachmentId);
        Assert.Equal($"/api/health/medicines/{medicineId}/attachments/{first.Id}", beforeSummary.Thumbnail.Url);

        using var setPrimary = await CapexApi.PutAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments/{second.Id}/primary",
            csrf);
        var marked = await setPrimary.Content.ReadFromJsonAsync<MedicineAttachmentResponse>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, setPrimary.StatusCode);
        Assert.Equal(second.Id, marked!.Id);
        Assert.True(marked.IsPrimary);

        var afterPrimary = await GetMedicineAsync(client, medicineId);
        var summary = await GetSummaryAsync(client, medicineId);
        Assert.Equal("primary", summary.Thumbnail.Source);
        Assert.Equal(second.Id, summary.Thumbnail.AttachmentId);
        Assert.True(afterPrimary.Attachments.Single(attachment => attachment.Id == second.Id).IsPrimary);
        Assert.False(afterPrimary.Attachments.Single(attachment => attachment.Id == first.Id).IsPrimary);

        using var deletePrimary = await CapexApi.DeleteAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments/{second.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, deletePrimary.StatusCode);

        var afterFallback = await GetSummaryAsync(client, medicineId);
        Assert.Equal("firstImage", afterFallback.Thumbnail.Source);
        Assert.Equal(first.Id, afterFallback.Thumbnail.AttachmentId);
        var fallbackDetail = await GetMedicineAsync(client, medicineId);
        Assert.False(Assert.Single(fallbackDetail.Attachments).IsPrimary);
    }

    [Fact]
    public async Task Non_image_attachments_cannot_be_primary_and_never_drive_the_thumbnail()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var medicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "Leaflet pill");

        var document = await UploadImageAsync(
            client,
            medicineId,
            "posology.txt",
            Encoding.UTF8.GetBytes("Take with food"),
            csrf,
            contentType: "text/plain");

        using var setPrimary = await CapexApi.PutAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments/{document.Id}/primary",
            csrf);
        var problem = await setPrimary.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var summary = await GetSummaryAsync(client, medicineId);
        var detail = await GetMedicineAsync(client, medicineId);

        Assert.Equal(HttpStatusCode.BadRequest, setPrimary.StatusCode);
        Assert.Equal("health.attachment.primary_invalid", problem!.Code);
        Assert.Equal("placeholder", summary.Thumbnail.Source);
        Assert.False(Assert.Single(detail.Attachments).IsPrimary);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_medicine()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var medicineId = await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        await server.CreateUserAsync("health-attacher", "HealthAttacher123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "health-attacher", "HealthAttacher123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync(
            $"/api/health/medicines/{medicineId}/attachments",
            CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member,
            $"/api/health/medicines/{medicineId}/attachments",
            "front.png",
            "image/png",
            PngBytes(1),
            memberCsrf);
        using var download = await member.GetAsync(
            $"/api/health/medicines/{medicineId}/attachments/1",
            CancellationToken.None);
        using var setPrimary = await CapexApi.PutAsync(
            member,
            $"/api/health/medicines/{medicineId}/attachments/1/primary",
            memberCsrf);
        using var delete = await CapexApi.DeleteAsync(
            member,
            $"/api/health/medicines/{medicineId}/attachments/1",
            memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, setPrimary.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Missing_and_invalid_attachments_return_health_problem_codes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var medicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId);

        using var missingDelete = await CapexApi.DeleteAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments/424242",
            csrf);
        using var missingPrimary = await CapexApi.PutAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments/424242/primary",
            csrf);
        using var invalid = await CapexApi.UploadAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var missingDeleteProblem = await missingDelete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var missingPrimaryProblem = await missingPrimary.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidProblem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
        Assert.Equal("health.attachment.not_found", missingDeleteProblem!.Code);
        Assert.Equal(HttpStatusCode.NotFound, missingPrimary.StatusCode);
        Assert.Equal("health.attachment.not_found", missingPrimaryProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("health.attachment.invalid", invalidProblem!.Code);
    }

    [Fact]
    public async Task Deleting_a_medicine_removes_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var medicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "Disposable");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments",
            "front.png",
            "image/png",
            PngBytes(1),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/health/medicines/{medicineId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            Assert.Empty(await attachments.ListByOwnerAsync(
                HealthAttachments.MedicineOwner(medicineId),
                CancellationToken.None));
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, HealthAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private static async Task<MedicineAttachmentResponse> UploadImageAsync(
        HttpClient client,
        int medicineId,
        string fileName,
        byte[] content,
        string? csrf,
        string contentType = "image/png")
    {
        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/health/medicines/{medicineId}/attachments",
            fileName,
            contentType,
            content,
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var created = await upload.Content.ReadFromJsonAsync<MedicineAttachmentResponse>(CancellationToken.None);
        return created!;
    }

    private static async Task<MedicineResponse> GetMedicineAsync(HttpClient client, int medicineId)
    {
        var medicine = await client.GetFromJsonAsync<MedicineResponse>(
            $"/api/health/medicines/{medicineId}",
            CancellationToken.None);
        Assert.NotNull(medicine);
        return medicine;
    }

    private static async Task<MedicineSummaryResponse> GetSummaryAsync(HttpClient client, int medicineId)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<MedicineSummaryResponse>>(
            $"/api/health/medicines?search={Uri.EscapeDataString("pill")}",
            CancellationToken.None);
        Assert.NotNull(page);
        return page.Items.Single(item => item.Id == medicineId);
    }

    private static byte[] PngBytes(byte marker) =>
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, marker];

    private sealed record ProblemPayload(string? Code);
}

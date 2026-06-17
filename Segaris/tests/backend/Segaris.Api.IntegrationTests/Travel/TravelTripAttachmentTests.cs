using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Travel;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Travel;

public sealed class TravelTripAttachmentTests
{
    [Fact]
    public async Task Attachments_round_trip_through_upload_list_detail_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Attachment trip");
        var content = Encoding.UTF8.GetBytes("Booking locator: ABC123");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/travel/trips/{tripId}/attachments",
            "booking.txt",
            "text/plain",
            content,
            csrf);
        var created = await upload.Content.ReadFromJsonAsync<AttachmentPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("booking.txt", created!.FileName);

        var list = await client.GetFromJsonAsync<AttachmentPayload[]>(
            $"/api/travel/trips/{tripId}/attachments",
            CancellationToken.None);
        var detail = await client.GetFromJsonAsync<TravelTripResponse>(
            $"/api/travel/trips/{tripId}",
            CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(list!).Id);
        Assert.Equal(created.Id, Assert.Single(detail!.Attachments).Id);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/travel/trips/{tripId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(
            client,
            $"/api/travel/trips/{tripId}/attachments/{created.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<AttachmentPayload[]>(
            $"/api/travel/trips/{tripId}/attachments",
            CancellationToken.None);
        Assert.Empty(empty!);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_trip()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        await server.CreateUserAsync("travel-attacher", "TravelAttacherPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "travel-attacher", "TravelAttacherPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync($"/api/travel/trips/{tripId}/attachments", CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member,
            $"/api/travel/trips/{tripId}/attachments",
            "booking.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
    }

    [Fact]
    public async Task Missing_and_invalid_attachments_return_travel_problem_codes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId);

        using var missing = await CapexApi.DeleteAsync(
            client,
            $"/api/travel/trips/{tripId}/attachments/424242",
            csrf);
        using var invalid = await CapexApi.UploadAsync(
            client,
            $"/api/travel/trips/{tripId}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var missingProblem = await missing.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidProblem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("travel.attachment.not_found", missingProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("travel.attachment.invalid", invalidProblem!.Code);
    }

    [Fact]
    public async Task Deleting_a_trip_removes_trip_and_expense_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(
            server.Services,
            founderId,
            expenses: [("EUR", 10m)]);
        var expenseId = await ExpenseIdAsync(server.Services, tripId);

        using var tripUpload = await CapexApi.UploadAsync(
            client,
            $"/api/travel/trips/{tripId}/attachments",
            "trip.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("Trip"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, tripUpload.StatusCode);
        await CreateExpenseAttachmentAsync(server.Services, expenseId, founderId);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/travel/trips/{tripId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            Assert.Empty(await attachments.ListByOwnerAsync(TravelAttachments.TripOwner(tripId), CancellationToken.None));
            Assert.Empty(await attachments.ListByOwnerAsync(TravelAttachments.ExpenseOwner(expenseId), CancellationToken.None));
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, TravelAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory)
            ? Directory.GetFiles(moduleDirectory)
            : [];
        Assert.Empty(files);
    }

    private static async Task<int> ExpenseIdAsync(IServiceProvider services, int tripId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<TravelExpense>()
            .Where(expense => expense.TripId == tripId)
            .Select(expense => expense.Id)
            .SingleAsync();
    }

    private static async Task CreateExpenseAttachmentAsync(
        IServiceProvider services,
        int expenseId,
        int userId)
    {
        await using var scope = services.CreateAsyncScope();
        var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Expense"));
        await attachments.CreateAsync(
            new(TravelAttachments.ExpenseOwner(expenseId), "expense.txt", "text/plain", stream),
            new UserId(userId),
            CancellationToken.None);
    }

    private sealed record AttachmentPayload(string Id, string FileName, string ContentType, long Size);

    private sealed record ProblemPayload(string? Code);
}

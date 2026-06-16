using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Opex;
using Segaris.Shared.Attachments;

namespace Segaris.Api.IntegrationTests.Opex;

public sealed class OpexOccurrenceAttachmentTests
{
    private const string MemberName = "occurrence-attacher";
    private const string MemberPassword = "AttacherPass123!";

    [Fact]
    public async Task Attachments_round_trip_through_upload_list_download_and_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (contractId, occurrenceId) = await CreateContractWithOccurrenceAsync(server, client, csrf);
        var baseRoute = $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}/attachments";
        var content = Encoding.UTF8.GetBytes("Receipt: 125.50 EUR");

        using var upload = await CapexApi.UploadAsync(
            client, baseRoute, "receipt.txt", "text/plain", content, csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var created = await upload.Content.ReadFromJsonAsync<AttachmentPayload>(CancellationToken.None);
        Assert.Equal("receipt.txt", created!.FileName);

        var list = await client.GetFromJsonAsync<AttachmentPayload[]>(baseRoute, CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(list!).Id);

        var downloaded = await client.GetByteArrayAsync($"{baseRoute}/{created.Id}", CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var delete = await CapexApi.DeleteAsync(client, $"{baseRoute}/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<AttachmentPayload[]>(baseRoute, CancellationToken.None);
        Assert.Empty(empty!);

        // The attachment also surfaces on the occurrence detail projection.
        var detail = await client.GetFromJsonAsync<OccurrenceWithAttachments>(
            $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}", CancellationToken.None);
        Assert.Empty(detail!.Attachments);
    }

    [Fact]
    public async Task Uploading_without_an_antiforgery_token_is_rejected()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (contractId, occurrenceId) = await CreateContractWithOccurrenceAsync(server, client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("data"),
            csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task Uploading_a_rejected_file_returns_the_opex_attachment_invalid_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (contractId, occurrenceId) = await CreateContractWithOccurrenceAsync(server, client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}/attachments",
            "malware.exe",
            "application/octet-stream",
            Encoding.UTF8.GetBytes("MZ"),
            csrf);
        var problem = await upload.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Equal("opex.attachment.invalid", problem!.Code);
    }

    [Fact]
    public async Task Attachment_routes_hide_an_inaccessible_private_contract()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var contractId = await OpexContractMutationTests.CreateContractAsync(
            server, founder, founderCsrf, builder => builder.WithVisibility("Private"));
        var occurrenceId = await OpexOccurrenceMutationTests.CreateOccurrenceAsync(
            founder, founderCsrf, contractId, b => b);

        await server.CreateUserAsync(MemberName, MemberPassword);
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, MemberName, MemberPassword);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var baseRoute = $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}/attachments";

        using var list = await member.GetAsync(baseRoute, CancellationToken.None);
        using var upload = await CapexApi.UploadAsync(
            member, baseRoute, "receipt.txt", "text/plain", Encoding.UTF8.GetBytes("data"), memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);
        var problem = await list.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("opex.contract.not_found", problem!.Code);
    }

    [Fact]
    public async Task A_missing_attachment_returns_the_opex_attachment_not_found_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (contractId, occurrenceId) = await CreateContractWithOccurrenceAsync(server, client, csrf);
        var baseRoute = $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}/attachments";

        using var download = await client.GetAsync($"{baseRoute}/424242", CancellationToken.None);
        using var delete = await CapexApi.DeleteAsync(client, $"{baseRoute}/424242", csrf);

        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var problem = await delete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("opex.attachment.not_found", problem!.Code);
    }

    [Fact]
    public async Task Deleting_an_occurrence_removes_its_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (contractId, occurrenceId) = await CreateContractWithOccurrenceAsync(server, client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("Receipt"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        using var deleted = await CapexApi.DeleteAsync(
            client, $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await AssertNoOccurrenceFilesAsync(server, occurrenceId);
    }

    [Fact]
    public async Task Deleting_a_contract_removes_its_occurrence_attachment_metadata_and_files()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var (contractId, occurrenceId) = await CreateContractWithOccurrenceAsync(server, client, csrf);

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/opex/contracts/{contractId}/occurrences/{occurrenceId}/attachments",
            "receipt.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("Receipt"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        // Deleting the parent contract cascades the occurrence rows and must also
        // reconcile the occurrence-level attachment files.
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/opex/contracts/{contractId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await AssertNoOccurrenceFilesAsync(server, occurrenceId);
    }

    private static async Task AssertNoOccurrenceFilesAsync(CapexTestServer server, int occurrenceId)
    {
        await using (var scope = server.Services.CreateAsyncScope())
        {
            var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
            var remaining = await attachments.ListByOwnerAsync(
                OpexAttachments.OccurrenceOwner(occurrenceId), CancellationToken.None);
            Assert.Empty(remaining);
        }

        var moduleDirectory = Path.Combine(server.AttachmentsPath, OpexAttachments.Module.ToLowerInvariant());
        var files = Directory.Exists(moduleDirectory) ? Directory.GetFiles(moduleDirectory) : [];
        Assert.Empty(files);
    }

    private static async Task<(int ContractId, int OccurrenceId)> CreateContractWithOccurrenceAsync(
        CapexTestServer server,
        HttpClient client,
        string csrf)
    {
        var contractId = await OpexContractMutationTests.CreateContractAsync(server, client, csrf, builder => builder);
        var occurrenceId = await OpexOccurrenceMutationTests.CreateOccurrenceAsync(client, csrf, contractId, b => b);
        return (contractId, occurrenceId);
    }

    private sealed record AttachmentPayload(string Id, string FileName, string ContentType, long Size);

    private sealed record OccurrenceWithAttachments(IReadOnlyList<AttachmentPayload> Attachments);

    private sealed record ProblemPayload(string? Code);
}

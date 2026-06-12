using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Segaris.Api.IntegrationTests.Attachments;

public sealed class AttachmentTests
{
    [Fact]
    public async Task Authenticated_upload_metadata_download_and_delete_work_end_to_end()
    {
        using var server = new AttachmentTestServer();
        using var client = server.CreateClient();
        await server.LoginAsync(client);

        var created = await UploadAsync(client, "notes.md", "text/markdown", "# Segaris\n"u8.ToArray());
        var id = created.GetProperty("id").GetProperty("value").GetInt32();

        var metadata = await client.GetFromJsonAsync<JsonElement>(
            $"/api/platform/attachments/{id}/metadata",
            CancellationToken.None);
        var download = await client.GetAsync($"/api/platform/attachments/{id}", CancellationToken.None);
        var csrf = await AttachmentTestServer.GetCsrfTokenAsync(client);
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/platform/attachments/{id}");
        deleteRequest.Headers.Add("X-CSRF-TOKEN", csrf);
        var deleted = await client.SendAsync(deleteRequest, CancellationToken.None);
        var missing = await client.GetAsync($"/api/platform/attachments/{id}/metadata", CancellationToken.None);

        Assert.Equal("notes.md", metadata.GetProperty("fileName").GetString());
        Assert.Equal("text/markdown", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal("# Segaris\n", await download.Content.ReadAsStringAsync(CancellationToken.None));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Empty(Directory.EnumerateFiles(server.AttachmentsPath, "*.md", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Attachment_endpoints_require_authentication_and_antiforgery()
    {
        using var server = new AttachmentTestServer();
        using var anonymous = server.CreateClient();
        var anonymousUpload = await SendUploadAsync(
            anonymous,
            "notes.txt",
            "text/plain",
            "hello"u8.ToArray(),
            csrf: null);

        using var authenticated = server.CreateClient();
        await server.LoginAsync(authenticated);
        var missingCsrf = await SendUploadAsync(
            authenticated,
            "notes.txt",
            "text/plain",
            "hello"u8.ToArray(),
            csrf: null);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousUpload.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
    }

    [Theory]
    [InlineData("payload.exe", "application/octet-stream", "MZ")]
    [InlineData("payload.json", "application/json", "{")]
    [InlineData("payload.xml", "application/xml", "<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///etc/passwd'>]><x>&e;</x>")]
    [InlineData("../escape.txt", "text/plain", "hello")]
    public async Task Unsafe_or_malformed_files_are_rejected(
        string fileName,
        string contentType,
        string content)
    {
        using var server = new AttachmentTestServer();
        using var client = server.CreateClient();
        await server.LoginAsync(client);
        var csrf = await AttachmentTestServer.GetCsrfTokenAsync(client);

        var response = await SendUploadAsync(
            client,
            fileName,
            contentType,
            Encoding.UTF8.GetBytes(content),
            csrf);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Mismatched_media_type_is_rejected()
    {
        using var server = new AttachmentTestServer();
        using var client = server.CreateClient();
        await server.LoginAsync(client);
        var csrf = await AttachmentTestServer.GetCsrfTokenAsync(client);

        var response = await SendUploadAsync(
            client,
            "notes.json",
            "text/plain",
            "{}"u8.ToArray(),
            csrf);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Structured_and_common_text_formats_are_accepted()
    {
        using var server = new AttachmentTestServer();
        using var client = server.CreateClient();
        await server.LoginAsync(client);

        var json = await UploadAsync(client, "data.json", "application/json", "{\"ok\":true}"u8.ToArray());
        var xml = await UploadAsync(client, "data.xml", "application/xml", "<root />"u8.ToArray());
        var yaml = await UploadAsync(client, "data.yaml", "application/yaml", "enabled: true\n"u8.ToArray());

        Assert.True(json.GetProperty("size").GetInt64() > 0);
        Assert.True(xml.GetProperty("size").GetInt64() > 0);
        Assert.True(yaml.GetProperty("size").GetInt64() > 0);
    }

    [Fact]
    public async Task Files_above_25_mib_are_rejected()
    {
        using var server = new AttachmentTestServer();
        using var client = server.CreateClient();
        await server.LoginAsync(client);
        var csrf = await AttachmentTestServer.GetCsrfTokenAsync(client);

        var response = await SendUploadAsync(
            client,
            "large.txt",
            "text/plain",
            new byte[(25 * 1024 * 1024) + 1],
            csrf);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(Directory.EnumerateFiles(server.AttachmentsPath, "*.txt", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Reconciliation_detects_unreferenced_staging_and_missing_files()
    {
        using var server = new AttachmentTestServer();
        using var client = server.CreateClient();
        await server.LoginAsync(client);

        var created = await UploadAsync(client, "notes.txt", "text/plain", "hello"u8.ToArray());
        var storedFile = Directory.EnumerateFiles(server.AttachmentsPath, "*.txt", SearchOption.AllDirectories).Single();
        File.Delete(storedFile);
        Directory.CreateDirectory(Path.Combine(server.AttachmentsPath, "platform"));
        await File.WriteAllTextAsync(
            Path.Combine(server.AttachmentsPath, "platform", "orphan.txt"),
            "orphan",
            CancellationToken.None);
        Directory.CreateDirectory(Path.Combine(server.AttachmentsPath, ".staging"));
        await File.WriteAllTextAsync(
            Path.Combine(server.AttachmentsPath, ".staging", "interrupted.upload"),
            "partial",
            CancellationToken.None);

        var result = await client.GetFromJsonAsync<JsonElement>(
            "/api/platform/attachments/reconciliation",
            CancellationToken.None);
        var unavailable = await client.GetAsync(
            $"/api/platform/attachments/{created.GetProperty("id").GetProperty("value").GetInt32()}",
            CancellationToken.None);

        Assert.Equal(1, result.GetProperty("missingFiles").GetInt32());
        Assert.Equal(1, result.GetProperty("unreferencedFiles").GetInt32());
        Assert.Equal(1, result.GetProperty("stagingFiles").GetInt32());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.True(created.GetProperty("id").GetProperty("value").GetInt32() > 0);
    }

    [Fact]
    public async Task Database_insert_failure_removes_the_compensating_physical_file()
    {
        using var server = new AttachmentTestServer();
        using var client = server.CreateClient();
        await server.LoginAsync(client);
        await CreateFailureTriggerAsync(
            server.DatabasePath,
            "fail_attachment_insert",
            "INSERT");
        var csrf = await AttachmentTestServer.GetCsrfTokenAsync(client);

        var response = await SendUploadAsync(
            client,
            "failure.txt",
            "text/plain",
            "failure"u8.ToArray(),
            csrf);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Empty(Directory.EnumerateFiles(server.AttachmentsPath, "*.txt", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Database_delete_failure_restores_the_physical_file_and_metadata()
    {
        using var server = new AttachmentTestServer();
        using var client = server.CreateClient();
        await server.LoginAsync(client);
        var created = await UploadAsync(client, "restore.txt", "text/plain", "restore"u8.ToArray());
        var id = created.GetProperty("id").GetProperty("value").GetInt32();
        var storedFile = Directory.EnumerateFiles(server.AttachmentsPath, "*.txt", SearchOption.AllDirectories).Single();
        await CreateFailureTriggerAsync(
            server.DatabasePath,
            "fail_attachment_delete",
            "DELETE");
        var csrf = await AttachmentTestServer.GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/platform/attachments/{id}");
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        var response = await client.SendAsync(request, CancellationToken.None);
        var metadata = await client.GetAsync(
            $"/api/platform/attachments/{id}/metadata",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(File.Exists(storedFile));
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
    }

    [Fact]
    public async Task Readiness_reports_healthy_for_writable_attachment_storage()
    {
        using var server = new AttachmentTestServer();
        using var client = server.CreateClient();

        var response = await client.GetAsync("/health/ready", CancellationToken.None);

        response.EnsureSuccessStatusCode();
        Assert.True(Directory.Exists(server.AttachmentsPath));
    }

    [Fact]
    public async Task Readiness_reports_unhealthy_for_an_inaccessible_storage_root()
    {
        var invalidRoot = Path.Combine(Path.GetTempPath(), $"segaris-storage-file-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(invalidRoot, "not a directory", CancellationToken.None);
        try
        {
            using var server = new AttachmentTestServer(invalidRoot);
            using var client = server.CreateClient();

            var response = await client.GetAsync("/health/ready", CancellationToken.None);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        finally
        {
            File.Delete(invalidRoot);
        }
    }

    private static async Task<JsonElement> UploadAsync(
        HttpClient client,
        string fileName,
        string contentType,
        byte[] content)
    {
        var csrf = await AttachmentTestServer.GetCsrfTokenAsync(client);
        var response = await SendUploadAsync(client, fileName, contentType, content, csrf);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None));
    }

    private static async Task<HttpResponseMessage> SendUploadAsync(
        HttpClient client,
        string fileName,
        string contentType,
        byte[] content,
        string? csrf)
    {
        using var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(file, "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/attachments")
        {
            Content = multipart,
        };
        if (csrf is not null)
        {
            request.Headers.Add("X-CSRF-TOKEN", csrf);
        }

        return await client.SendAsync(request, CancellationToken.None);
    }

    private static async Task CreateFailureTriggerAsync(
        string databasePath,
        string triggerName,
        string operation)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TRIGGER {triggerName}
            BEFORE {operation} ON platform_attachments
            BEGIN
                SELECT RAISE(ABORT, 'intentional attachment test failure');
            END;
            """;
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }
}

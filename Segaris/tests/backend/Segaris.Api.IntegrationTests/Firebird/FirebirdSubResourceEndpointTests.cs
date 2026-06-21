using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Firebird;

public sealed class FirebirdSubResourceEndpointTests
{
    [Fact]
    public async Task Username_crud_supports_repeated_platforms_and_validation()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var personId = await FirebirdTestData.SeedPersonAsync(server.Services, founderId, name: "Usernamed");
        var emailId = await FirebirdTestData.PlatformIdAsync(server.Services, "Email");
        var discordId = await FirebirdTestData.PlatformIdAsync(server.Services, "Discord");

        using var first = await CapexApi.PostJsonAsync(
            client,
            $"/api/people/{personId}/usernames",
            new UsernameRequest(emailId, "  ada@example.test  ", "  Primary email  "),
            csrf);
        var created = await first.Content.ReadFromJsonAsync<UsernameResponse>(CancellationToken.None);
        using var repeated = await CapexApi.PostJsonAsync(
            client,
            $"/api/people/{personId}/usernames",
            new UsernameRequest(emailId, "ada.alt@example.test", null),
            csrf);
        using var updated = await CapexApi.PutJsonAsync(
            client,
            $"/api/people/{personId}/usernames/{created!.Id}",
            new UsernameRequest(discordId, "ada#1234", ""),
            csrf);
        var updateBody = await updated.Content.ReadFromJsonAsync<UsernameResponse>(CancellationToken.None);
        var list = await client.GetFromJsonAsync<UsernameResponse[]>(
            $"/api/people/{personId}/usernames",
            CancellationToken.None);
        var detail = await client.GetFromJsonAsync<PersonResponse>(
            $"/api/people/{personId}",
            CancellationToken.None);
        using var invalidPlatform = await CapexApi.PostJsonAsync(
            client,
            $"/api/people/{personId}/usernames",
            new UsernameRequest(999_999, "missing", null),
            csrf);
        using var invalidHandle = await CapexApi.PostJsonAsync(
            client,
            $"/api/people/{personId}/usernames",
            new UsernameRequest(emailId, "   ", null),
            csrf);
        using var deleted = await CapexApi.DeleteAsync(
            client,
            $"/api/people/{personId}/usernames/{created.Id}",
            csrf);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal("ada@example.test", created.Handle);
        Assert.Equal("Primary email", created.Notes);
        Assert.Equal(HttpStatusCode.Created, repeated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Discord", updateBody!.PlatformName);
        Assert.Null(updateBody.Notes);
        Assert.Equal(2, list!.Length);
        Assert.Equal(2, detail!.Usernames.Count);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPlatform.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidHandle.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await FirebirdTestData.UsernameExistsAsync(server.Services, created.Id));
    }

    [Fact]
    public async Task Interaction_crud_orders_descending_and_rejects_future_dates()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var personId = await FirebirdTestData.SeedPersonAsync(server.Services, founderId, name: "Talked");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        using var older = await CapexApi.PostJsonAsync(
            client,
            $"/api/people/{personId}/interactions",
            new InteractionRequest(today.AddDays(-10), "  First message  "),
            csrf);
        var oldBody = await older.Content.ReadFromJsonAsync<InteractionResponse>(CancellationToken.None);
        using var newer = await CapexApi.PostJsonAsync(
            client,
            $"/api/people/{personId}/interactions",
            new InteractionRequest(today.AddDays(-1), "Recent call"),
            csrf);
        var list = await client.GetFromJsonAsync<InteractionResponse[]>(
            $"/api/people/{personId}/interactions",
            CancellationToken.None);
        using var updated = await CapexApi.PutJsonAsync(
            client,
            $"/api/people/{personId}/interactions/{oldBody!.Id}",
            new InteractionRequest(today.AddDays(-2), "Updated message"),
            csrf);
        using var future = await CapexApi.PostJsonAsync(
            client,
            $"/api/people/{personId}/interactions",
            new InteractionRequest(today.AddDays(1), "Future meeting"),
            csrf);
        using var deleted = await CapexApi.DeleteAsync(
            client,
            $"/api/people/{personId}/interactions/{oldBody.Id}",
            csrf);

        Assert.Equal(HttpStatusCode.Created, older.StatusCode);
        Assert.Equal("First message", oldBody.Description);
        Assert.Equal(HttpStatusCode.Created, newer.StatusCode);
        Assert.Equal(["Recent call", "First message"], list!.Select(item => item.Description).ToArray());
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await FirebirdTestData.InteractionExistsAsync(server.Services, oldBody.Id));
    }

    [Fact]
    public async Task Avatar_upload_replace_download_delete_reject_and_person_cleanup_work()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var personId = await FirebirdTestData.SeedPersonAsync(server.Services, founderId, name: "Avatar");

        using var first = await PutAvatarAsync(client, personId, "avatar.png", "image/png", PngBytes(1), csrf);
        var firstBody = await first.Content.ReadFromJsonAsync<PersonAvatarResponse>(CancellationToken.None);
        using var firstDownload = await client.GetAsync($"/api/people/{personId}/avatar", CancellationToken.None);
        var firstBytes = await firstDownload.Content.ReadAsByteArrayAsync(CancellationToken.None);
        using var replacement = await PutAvatarAsync(client, personId, "avatar.webp", "image/webp", WebpBytes(), csrf);
        var detail = await client.GetFromJsonAsync<PersonResponse>($"/api/people/{personId}", CancellationToken.None);
        using var secondDownload = await client.GetAsync($"/api/people/{personId}/avatar", CancellationToken.None);
        using var rejected = await PutAvatarAsync(client, personId, "notes.txt", "text/plain", [1, 2, 3], csrf);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/people/{personId}/avatar", csrf);
        using var missing = await client.GetAsync($"/api/people/{personId}/avatar", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("avatar", firstBody!.Source);
        Assert.Equal($"/api/people/{personId}/avatar", firstBody.Url);
        Assert.Equal("image/png", firstDownload.Content.Headers.ContentType!.MediaType);
        Assert.Equal(PngBytes(1), firstBytes);
        Assert.Equal(HttpStatusCode.OK, replacement.StatusCode);
        Assert.Equal("avatar", detail!.Avatar.Source);
        Assert.NotEqual(firstBody.AttachmentId, detail.Avatar.AttachmentId);
        Assert.Equal("image/webp", secondDownload.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Empty(Directory.EnumerateFiles(server.AttachmentsPath, "*", SearchOption.AllDirectories));

        using var uploadedForDelete = await PutAvatarAsync(client, personId, "cleanup.png", "image/png", PngBytes(2), csrf);
        using var deletePerson = await CapexApi.DeleteAsync(client, $"/api/people/{personId}", csrf);

        Assert.Equal(HttpStatusCode.OK, uploadedForDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deletePerson.StatusCode);
        Assert.Empty(Directory.EnumerateFiles(server.AttachmentsPath, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Subresources_and_avatar_follow_public_collaboration_and_private_isolation()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicPersonId = await FirebirdTestData.SeedPersonAsync(server.Services, founderId, name: "Shared", visibility: RecordVisibility.Public);
        var privatePersonId = await FirebirdTestData.SeedPersonAsync(server.Services, founderId, name: "Hidden", visibility: RecordVisibility.Private);
        var emailId = await FirebirdTestData.PlatformIdAsync(server.Services, "Email");

        await server.CreateUserAsync("firebird-member", "FirebirdMember123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "firebird-member", "FirebirdMember123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var publicUsername = await CapexApi.PostJsonAsync(
            member,
            $"/api/people/{publicPersonId}/usernames",
            new UsernameRequest(emailId, "member@example.test", null),
            csrf);
        using var privateUsername = await CapexApi.PostJsonAsync(
            member,
            $"/api/people/{privatePersonId}/usernames",
            new UsernameRequest(emailId, "hidden@example.test", null),
            csrf);
        using var publicInteraction = await CapexApi.PostJsonAsync(
            member,
            $"/api/people/{publicPersonId}/interactions",
            new InteractionRequest(DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-1), "Shared note"),
            csrf);
        using var privateInteractionList = await member.GetAsync(
            $"/api/people/{privatePersonId}/interactions",
            CancellationToken.None);
        using var publicAvatar = await PutAvatarAsync(member, publicPersonId, "member.png", "image/png", PngBytes(3), csrf);
        using var privateAvatar = await PutAvatarAsync(member, privatePersonId, "hidden.png", "image/png", PngBytes(4), csrf);

        Assert.Equal(HttpStatusCode.Created, publicUsername.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, privateUsername.StatusCode);
        Assert.Equal(HttpStatusCode.Created, publicInteraction.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, privateInteractionList.StatusCode);
        Assert.Equal(HttpStatusCode.OK, publicAvatar.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, privateAvatar.StatusCode);
    }

    private static async Task<HttpResponseMessage> PutAvatarAsync(
        HttpClient client,
        int personId,
        string fileName,
        string contentType,
        byte[] content,
        string csrf)
    {
        using var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(file, "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/people/{personId}/avatar")
        {
            Content = multipart,
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }

    private static byte[] PngBytes(byte marker) =>
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, marker];

    private static byte[] WebpBytes() =>
        [0x52, 0x49, 0x46, 0x46, 0x04, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];
}

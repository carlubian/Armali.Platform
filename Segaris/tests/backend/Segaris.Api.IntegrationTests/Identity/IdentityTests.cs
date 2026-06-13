using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Segaris.Api.IntegrationTests.Identity;

public sealed class IdentityTests
{
    [Fact]
    public async Task Bootstrap_administrator_can_authenticate_and_read_its_session()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();

        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session", CancellationToken.None);

        Assert.Equal(IdentityTestServer.AdminUserName, session.GetProperty("userName").GetString());
        Assert.Contains(
            session.GetProperty("roles").EnumerateArray().Select(role => role.GetString()),
            role => role == "Admin");
        Assert.Equal(IdentityTestServer.AdminUserName, session.GetProperty("displayName").GetString());
        Assert.Equal("en-GB", session.GetProperty("language").GetString());
    }

    [Fact]
    public async Task Current_user_can_read_and_update_their_profile()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        var initial = await client.GetFromJsonAsync<JsonElement>("/api/session/profile", CancellationToken.None);
        using var updated = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            "/api/session/profile",
            new { displayName = "  Household Admin  ", language = "en-GB" });
        var body = await updated.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session", CancellationToken.None);

        Assert.Equal(IdentityTestServer.AdminUserName, initial.GetProperty("displayName").GetString());
        Assert.Equal("Household Admin", body.GetProperty("displayName").GetString());
        Assert.Equal("Household Admin", session.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Profile_updates_reject_unsupported_languages_and_require_antiforgery()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        using var unsupported = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            "/api/session/profile",
            new { displayName = "Founder", language = "es-ES" });
        using var missingCsrf = await client.PutAsJsonAsync(
            "/api/session/profile",
            new { displayName = "Founder", language = "en-GB" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, unsupported.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
    }

    [Fact]
    public async Task Avatar_upload_replace_download_and_delete_work()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        using var first = await PutAvatarAsync(client, "avatar.png", "image/png", PngBytes(1));
        var profile = await client.GetFromJsonAsync<JsonElement>("/api/session/profile", CancellationToken.None);
        using var firstDownload = await client.GetAsync("/api/session/profile/avatar", CancellationToken.None);
        using var replacement = await PutAvatarAsync(client, "avatar.webp", "image/webp", WebpBytes());
        using var secondDownload = await client.GetAsync("/api/session/profile/avatar", CancellationToken.None);
        using var deleted = await SendWithCsrfAsync(client, HttpMethod.Delete, "/api/session/profile/avatar");
        using var missing = await client.GetAsync("/api/session/profile/avatar", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("/api/users/1/avatar", profile.GetProperty("avatarUrl").GetString());
        Assert.Equal("image/png", firstDownload.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.OK, replacement.StatusCode);
        Assert.Equal("image/webp", secondDownload.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Empty(Directory.EnumerateFiles(server.AttachmentsPath, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Authenticated_users_can_read_but_cannot_modify_another_users_avatar()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var memberId = await CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");
        using var uploaded = await PutAvatarAsync(member, "member.png", "image/png", PngBytes(2));
        using var sharedRead = await admin.GetAsync($"/api/users/{memberId}/avatar", CancellationToken.None);
        using var forbiddenWrite = await SendWithCsrfAsync(
            admin,
            HttpMethod.Delete,
            $"/api/users/{memberId}/avatar");

        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sharedRead.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, forbiddenWrite.StatusCode);
    }

    [Fact]
    public async Task Avatar_uploads_reject_non_image_files()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        using var response = await PutAvatarAsync(
            client,
            "document.pdf",
            "application/pdf",
            "%PDF-1.7"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_session_cookie_is_http_only_and_strict_same_site()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/session",
            new { userName = IdentityTestServer.AdminUserName, password = IdentityTestServer.AdminPassword },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
        var setCookie = string.Join(
            "; ",
            response.Headers.TryGetValues("Set-Cookie", out var values) ? values : []);

        Assert.Contains("segaris.session", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_failures_are_generic_for_unknown_and_incorrect_credentials()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();

        var unknown = await client.PostAsJsonAsync(
            "/api/session",
            new { userName = "ghost", password = "WhateverPass123!" },
            CancellationToken.None);
        var wrongPassword = await client.PostAsJsonAsync(
            "/api/session",
            new { userName = IdentityTestServer.AdminUserName, password = "WrongPassword123!" },
            CancellationToken.None);

        var unknownProblem = await unknown.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        var wrongProblem = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal("authentication.required", unknownProblem.GetProperty("code").GetString());
        Assert.Equal(
            unknownProblem.GetProperty("title").GetString(),
            wrongProblem.GetProperty("title").GetString());
        Assert.DoesNotContain("ghost", wrongProblem.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            IdentityTestServer.AdminUserName,
            unknownProblem.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Five_failed_attempts_lock_the_account_even_for_correct_credentials()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failure = await client.PostAsJsonAsync(
                "/api/session",
                new { userName = IdentityTestServer.AdminUserName, password = "WrongPassword123!" },
                CancellationToken.None);
            Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        }

        using var locked = await client.PostAsJsonAsync(
            "/api/session",
            new { userName = IdentityTestServer.AdminUserName, password = IdentityTestServer.AdminPassword },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [Fact]
    public async Task Logout_removes_the_session()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();

        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        using var logout = await SendWithCsrfAsync(client, HttpMethod.Delete, "/api/session");
        using var afterLogout = await client.GetAsync("/api/session", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Administrators_create_users_who_appear_in_the_paginated_list()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        await CreateUserAsync(admin, "alice", "AlicePass1234!", "User");
        await CreateUserAsync(admin, "bob", "BobPass123456!", "Admin");

        var list = await admin.GetFromJsonAsync<JsonElement>(
            "/api/admin/users?page=1&pageSize=25&sort=userName&sortDirection=asc",
            CancellationToken.None);

        Assert.Equal(3, list.GetProperty("totalCount").GetInt32());
        var userNames = list.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("userName").GetString())
            .ToArray();
        Assert.Contains("alice", userNames);
        Assert.Contains("bob", userNames);
        var first = list.GetProperty("items").EnumerateArray().First();
        Assert.True(first.TryGetProperty("isActive", out _));
        Assert.True(first.TryGetProperty("createdAt", out _));
        Assert.True(first.TryGetProperty("roles", out _));
    }

    [Fact]
    public async Task Administrative_endpoints_require_the_admin_role()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        await CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");

        using var forbidden = await member.GetAsync("/api/admin/users", CancellationToken.None);
        using var allowed = await admin.GetAsync("/api/admin/users", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Deactivating_a_user_invalidates_active_sessions()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var memberId = await CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");
        using var beforeDeactivation = await member.GetAsync("/api/session", CancellationToken.None);

        using var deactivate = await SendWithCsrfAsync(
            admin,
            HttpMethod.Post,
            $"/api/admin/users/{memberId}/deactivate");
        using var afterDeactivation = await member.GetAsync("/api/session", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, beforeDeactivation.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterDeactivation.StatusCode);
    }

    [Fact]
    public async Task A_user_can_change_their_password_and_the_old_one_stops_working()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        await CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");
        using var change = await SendWithCsrfAsync(
            member,
            HttpMethod.Post,
            "/api/session/password",
            new { currentPassword = "MemberPass123!", newPassword = "MemberPass789!" });

        // The current session remains valid after the user changes their own password.
        using var stillAuthenticated = await member.GetAsync("/api/session", CancellationToken.None);

        using var oldLogin = server.CreateClient();
        using var oldAttempt = await oldLogin.PostAsJsonAsync(
            "/api/session",
            new { userName = "member", password = "MemberPass123!" },
            CancellationToken.None);

        using var newLogin = server.CreateClient();
        using var newAttempt = await newLogin.PostAsJsonAsync(
            "/api/session",
            new { userName = "member", password = "MemberPass789!" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        Assert.Equal(HttpStatusCode.OK, stillAuthenticated.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, newAttempt.StatusCode);
    }

    [Fact]
    public async Task Authenticated_writes_are_rejected_without_an_antiforgery_token()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        using var response = await client.PostAsJsonAsync(
            "/api/session/password",
            new { currentPassword = IdentityTestServer.AdminPassword, newPassword = "RotatedPass123!" },
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.invalid", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Authenticated_writes_succeed_with_a_valid_antiforgery_token()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/session/password",
            new { currentPassword = IdentityTestServer.AdminPassword, newPassword = "RotatedPass123!" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Administrative_credential_recovery_resets_the_password_and_invalidates_sessions()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var memberId = await CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");

        using var recovery = await SendWithCsrfAsync(
            admin,
            HttpMethod.Post,
            $"/api/admin/users/{memberId}/password",
            new { newPassword = "RecoveredPass123!" });
        using var afterRecovery = await member.GetAsync("/api/session", CancellationToken.None);

        using var oldLogin = server.CreateClient();
        using var oldAttempt = await oldLogin.PostAsJsonAsync(
            "/api/session",
            new { userName = "member", password = "MemberPass123!" },
            CancellationToken.None);

        using var newLogin = server.CreateClient();
        using var newAttempt = await newLogin.PostAsJsonAsync(
            "/api/session",
            new { userName = "member", password = "RecoveredPass123!" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, recovery.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterRecovery.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, newAttempt.StatusCode);
    }

    [Fact]
    public async Task Administrators_edit_a_member_display_name_and_role()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var memberId = await CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var response = await SendWithCsrfAsync(
            admin,
            HttpMethod.Put,
            $"/api/admin/users/{memberId}",
            new { displayName = "Renamed Member", role = "Admin" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed Member", body.GetProperty("displayName").GetString());
        var roles = body.GetProperty("roles")
            .EnumerateArray()
            .Select(role => role.GetString())
            .ToArray();
        Assert.Contains("Admin", roles);
        Assert.DoesNotContain("User", roles);
    }

    [Fact]
    public async Task Administrators_can_edit_their_own_display_name_but_not_their_own_role()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        var session = await admin.GetFromJsonAsync<JsonElement>("/api/session", CancellationToken.None);
        var adminId = session.GetProperty("userId").GetInt32();

        // Renaming yourself while keeping your role is allowed.
        using var rename = await SendWithCsrfAsync(
            admin,
            HttpMethod.Put,
            $"/api/admin/users/{adminId}",
            new { displayName = "Household Admin", role = "Admin" });
        var renamed = await rename.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        // Changing your own role is rejected, so the household cannot lock itself out.
        using var demote = await SendWithCsrfAsync(
            admin,
            HttpMethod.Put,
            $"/api/admin/users/{adminId}",
            new { displayName = "Household Admin", role = "User" });
        var problem = await demote.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        Assert.Equal("Household Admin", renamed.GetProperty("displayName").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, demote.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty("role", out _));
    }

    [Fact]
    public async Task Editing_a_user_rejects_a_blank_display_name()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var memberId = await CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var response = await SendWithCsrfAsync(
            admin,
            HttpMethod.Put,
            $"/api/admin/users/{memberId}",
            new { displayName = "   ", role = "User" });
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty("displayName", out _));
    }

    [Fact]
    public async Task Updating_a_user_is_rejected_without_an_antiforgery_token()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var memberId = await CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var response = await admin.PutAsJsonAsync(
            $"/api/admin/users/{memberId}",
            new { displayName = "Renamed", role = "Admin" },
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.invalid", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Non_administrators_cannot_update_users()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var memberId = await CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");

        using var response = await SendWithCsrfAsync(
            member,
            HttpMethod.Put,
            $"/api/admin/users/{memberId}",
            new { displayName = "Renamed", role = "Admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<int> CreateUserAsync(
        HttpClient admin,
        string userName,
        string password,
        string role)
    {
        using var response = await SendWithCsrfAsync(
            admin,
            HttpMethod.Post,
            "/api/admin/users",
            new { userName, password, role });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        return body.GetProperty("id").GetInt32();
    }

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body = null)
    {
        var csrf = await IdentityTestServer.GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, CancellationToken.None);
    }

    private static async Task<HttpResponseMessage> PutAvatarAsync(
        HttpClient client,
        string fileName,
        string contentType,
        byte[] content)
    {
        var csrf = await IdentityTestServer.GetCsrfTokenAsync(client);
        using var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(file, "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/session/profile/avatar")
        {
            Content = multipart,
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }

    private static byte[] PngBytes(byte marker) =>
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, marker];

    private static byte[] WebpBytes() =>
        [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50];
}

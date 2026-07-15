using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Identity;

/// <summary>
/// A key carries its user's identity into the existing authorization policies and
/// nothing more. These tests use real Capex records, because the claim under test
/// is that the module's own visibility filter applies to a key untouched.
/// </summary>
public sealed class ApiKeyPrivacyTests
{
    private const string MemberName = "member";
    private const string MemberPassword = "MemberPass123!";

    [Fact]
    public async Task An_admin_key_cannot_read_another_users_private_entries()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync(MemberName, MemberPassword);
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        await CapexTestData.SeedEntryAsync(
            server.Services,
            memberId,
            title: "Member private",
            visibility: RecordVisibility.Private);
        await CapexTestData.SeedEntryAsync(
            server.Services,
            adminId,
            title: "Admin private",
            visibility: RecordVisibility.Private);
        var sharedId = await CapexTestData.SeedEntryAsync(
            server.Services,
            memberId,
            title: "Shared public",
            visibility: RecordVisibility.Public);

        using var admin = await server.CreateAuthenticatedClientAsync();
        var token = await IssueKeyAsync(admin);

        using var agent = server.CreateClient(handleCookies: false);
        using var listed = await ApiKeyTests.SendWithKeyAsync(
            agent,
            HttpMethod.Get,
            "/api/capex/entries?page=1&pageSize=25",
            token);
        var page = await listed.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        var titles = page.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("title").GetString())
            .ToArray();

        // The Admin role grants no bypass of the creator-only rule, through a key or otherwise.
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.DoesNotContain("Member private", titles);
        Assert.Contains("Admin private", titles);
        Assert.Contains("Shared public", titles);
        Assert.DoesNotContain("Member private", page.ToString(), StringComparison.Ordinal);
        Assert.True(sharedId > 0);
    }

    [Fact]
    public async Task An_admin_key_cannot_read_another_users_private_entry_by_id()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync(MemberName, MemberPassword);
        var privateId = await CapexTestData.SeedEntryAsync(
            server.Services,
            memberId,
            title: "Member private",
            visibility: RecordVisibility.Private);

        using var admin = await server.CreateAuthenticatedClientAsync();
        var token = await IssueKeyAsync(admin);

        using var agent = server.CreateClient(handleCookies: false);
        using var response = await ApiKeyTests.SendWithKeyAsync(
            agent,
            HttpMethod.Get,
            $"/api/capex/entries/{privateId}",
            token);

        // A hidden record is indistinguishable from an absent one.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_members_key_reaches_that_members_own_private_entries()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync(MemberName, MemberPassword);
        var privateId = await CapexTestData.SeedEntryAsync(
            server.Services,
            memberId,
            title: "Member private",
            visibility: RecordVisibility.Private);

        using var member = await server.CreateAuthenticatedClientAsync(MemberName, MemberPassword);
        var token = await IssueKeyAsync(member);

        using var agent = server.CreateClient(handleCookies: false);
        using var response = await ApiKeyTests.SendWithKeyAsync(
            agent,
            HttpMethod.Get,
            $"/api/capex/entries/{privateId}",
            token);
        var entry = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        // A key is the bound user's identity: it reaches exactly what their session reaches.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Member private", entry.GetProperty("title").GetString());
    }

    private static async Task<string> IssueKeyAsync(HttpClient client)
    {
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/profile/api-keys")
        {
            Content = JsonContent.Create(new { name = "Agent" }),
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request, CancellationToken.None);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        return body.GetProperty("token").GetString()!;
    }
}

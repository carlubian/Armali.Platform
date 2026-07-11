using System.Net;
using System.Net.Http.Json;
using Blackwing.Persistence.Identity;
using Blackwing.Shared.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Blackwing.Api.Tests;

/// <summary>
/// End-to-end coverage over HTTP of the admin-only account management surface:
/// the create → list → rename → reset-password → delete lifecycle, that the
/// surface is closed to non-admins, and the guards that stop an administrator
/// from locking themselves out.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AccountManagementTests(PostgresFixture fixture)
{
    private const string Password = "Test-Password-1";
    private const string NewPassword = "Fresh-Password-2";

    [Fact]
    public async Task Admin_can_create_list_rename_reset_and_delete_accounts()
    {
        if (!fixture.Available) return;
        var admin = await SignInNewUserAsync(admin: true);

        // Create a plain account: it lands as a User and shows up in the listing.
        var username = $"acc-{Guid.NewGuid():N}";
        var created = await CreateAsync(admin, username, Password);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var account = (await created.Content.ReadFromJsonAsync<AccountDto>())!;
        Assert.Equal(username, account.Username);
        Assert.Equal(BlackwingRoles.User, account.Role);
        Assert.Contains((await admin.GetFromJsonAsync<AccountListDto>("/api/admin/accounts/"))!.Accounts, item => item.Id == account.Id);

        // Rename it, then confirm the new name is reflected in the listing.
        var renamed = $"{username}-renamed";
        var rename = await PutAsync(admin, $"/api/admin/accounts/{account.Id}", new { username = renamed });
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        Assert.Contains((await admin.GetFromJsonAsync<AccountListDto>("/api/admin/accounts/"))!.Accounts, item => item.Username == renamed);

        // Reset the password; the account can then sign in with the new one but not the old.
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsync(admin, $"/api/admin/accounts/{account.Id}/password", new { password = NewPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(renamed, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(renamed, NewPassword)).StatusCode);

        // Delete it; it is gone from the listing.
        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(admin, $"/api/admin/accounts/{account.Id}")).StatusCode);
        Assert.DoesNotContain((await admin.GetFromJsonAsync<AccountListDto>("/api/admin/accounts/"))!.Accounts, item => item.Id == account.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(admin, $"/api/admin/accounts/{account.Id}")).StatusCode);
    }

    [Fact]
    public async Task A_duplicate_username_is_rejected_on_rename()
    {
        if (!fixture.Available) return;
        var admin = await SignInNewUserAsync(admin: true);
        var first = $"acc-{Guid.NewGuid():N}";
        var second = $"acc-{Guid.NewGuid():N}";
        await CreateAsync(admin, first, Password);
        var target = (await (await CreateAsync(admin, second, Password)).Content.ReadFromJsonAsync<AccountDto>())!;

        // Renaming the second account onto the first's name collides.
        var response = await PutAsync(admin, $"/api/admin/accounts/{target.Id}", new { username = first });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_account_surface_is_admin_only()
    {
        if (!fixture.Available) return;
        var user = await SignInNewUserAsync();
        var anonymous = fixture.Factory!.CreateClient(new() { AllowAutoRedirect = false });

        // A signed-in non-admin is forbidden the whole surface.
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/admin/accounts/")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await CreateAsync(user, $"acc-{Guid.NewGuid():N}", Password)).StatusCode);

        // An anonymous caller is challenged.
        Assert.True((await anonymous.GetAsync("/api/admin/accounts/")).StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Found);
    }

    [Fact]
    public async Task An_admin_cannot_delete_their_own_account()
    {
        if (!fixture.Available) return;
        var (admin, adminId) = await SignInNewUserWithIdAsync(admin: true);
        var response = await DeleteAsync(admin, $"/api/admin/accounts/{adminId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        // The account survives the refused deletion.
        Assert.Contains((await admin.GetFromJsonAsync<AccountListDto>("/api/admin/accounts/"))!.Accounts, item => item.Id == adminId);
    }

    // --- helpers ---

    private async Task<HttpClient> SignInNewUserAsync(bool admin = false) =>
        (await SignInNewUserWithIdAsync(admin)).Client;

    private async Task<(HttpClient Client, Guid Id)> SignInNewUserWithIdAsync(bool admin = false)
    {
        var username = $"user-{Guid.NewGuid():N}";
        var id = Guid.NewGuid();
        await using (var scope = fixture.Factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<BlackwingUser>>();
            var user = new BlackwingUser { Id = id, UserName = username };
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
            await users.AddToRoleAsync(user, admin ? BlackwingRoles.Admin : BlackwingRoles.User);
        }

        var client = fixture.Factory!.CreateClient();
        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(username, Password, client)).StatusCode);
        return (client, id);
    }

    private async Task<HttpResponseMessage> LoginAsync(string username, string password, HttpClient? client = null)
    {
        client ??= fixture.Factory!.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { username, password }) };
        request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private static async Task<string> CsrfAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<AntiforgeryDto>("/api/auth/antiforgery"))!.RequestToken;

    private Task<HttpResponseMessage> CreateAsync(HttpClient client, string username, string password) =>
        PostAsync(client, "/api/admin/accounts/", new { username, password });

    private async Task<HttpResponseMessage> PostAsync(HttpClient client, string uri, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAsync(HttpClient client, string uri, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, uri) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAsync(HttpClient client, string uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private sealed record AntiforgeryDto(string RequestToken);
    private sealed record AccountDto(Guid Id, string Username, string Role);
    private sealed record AccountListDto(IReadOnlyList<AccountDto> Accounts);
}

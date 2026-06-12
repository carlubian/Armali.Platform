using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Segaris.Api.IntegrationTests.Identity;

namespace Segaris.Api.IntegrationTests.Observability;

public sealed class FrontendDiagnosticsTests
{
    [Fact]
    public async Task Diagnostics_require_authentication_and_antiforgery()
    {
        using var server = new IdentityTestServer();
        using var anonymous = server.CreateClient();
        using var authenticated = server.CreateClient();

        using var unauthorized = await anonymous.PostAsJsonAsync(
            "/api/diagnostics/frontend",
            ValidDiagnostic(),
            CancellationToken.None);

        await IdentityTestServer.LoginAsync(
            authenticated,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);
        using var missingCsrf = await authenticated.PostAsJsonAsync(
            "/api/diagnostics/frontend",
            ValidDiagnostic(),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_accept_a_fixed_valid_schema_and_return_the_server_trace_id()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var csrf = await IdentityTestServer.GetCsrfTokenAsync(client);

        using var response = await SendAsync(client, csrf, ValidDiagnostic());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(
            response.Headers.GetValues("X-Trace-ID").Single(),
            body.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Diagnostics_reject_invalid_schema_and_oversized_payloads()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var csrf = await IdentityTestServer.GetCsrfTokenAsync(client);

        using var invalid = await SendAsync(client, csrf, new
        {
            eventCode = "invalid code with spaces",
            severity = "Debug",
            message = "",
        });
        using var oversized = await SendAsync(client, csrf, new
        {
            eventCode = "app.failure",
            severity = "Error",
            message = new string('x', 17 * 1024),
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_are_rate_limited_per_client()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var csrf = await IdentityTestServer.GetCsrfTokenAsync(client);

        for (var request = 0; request < 30; request++)
        {
            using var accepted = await SendAsync(client, csrf, ValidDiagnostic());
            Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        }

        using var limited = await SendAsync(client, csrf, ValidDiagnostic());

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        var problem = await limited.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        Assert.Equal("request.rate_limited", problem.GetProperty("code").GetString());
    }

    private static object ValidDiagnostic() => new
    {
        eventCode = "app.initialization.failed",
        severity = "Error",
        message = "The application failed to initialize.",
        stack = "at bootstrap",
        route = "/",
        component = "AppRoot",
        clientTraceId = "browser-trace-1",
    };

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string csrf,
        object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/diagnostics/frontend")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }
}

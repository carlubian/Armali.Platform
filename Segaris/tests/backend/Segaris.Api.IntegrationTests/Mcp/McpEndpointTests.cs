using System.Net;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Segaris.Api.Platform.Mcp;
using Segaris.Api.IntegrationTests.Identity;

namespace Segaris.Api.IntegrationTests.Mcp;

public sealed class McpEndpointTests
{
    [Fact]
    public async Task Mcp_endpoint_is_not_mapped_when_feature_flag_is_disabled()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient(handleCookies: false);

        using var response = await client.GetAsync("/mcp", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_endpoint_does_not_accept_cookie_authentication()
    {
        using var server = new IdentityTestServer(mcpEnabled: true);
        using var browser = server.CreateClient();
        await IdentityTestServer.LoginAsync(browser, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        using var response = await browser.GetAsync("/mcp", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_identity_tool_authenticates_with_api_key()
    {
        using var server = new IdentityTestServer(mcpEnabled: true);
        using var browser = server.CreateClient();
        await IdentityTestServer.LoginAsync(browser, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var token = (await ApiKeyTests.CreateKeyAsync(browser, "MCP client"))
            .GetProperty("token")
            .GetString()!;

        using var http = server.CreateClient(handleCookies: false);
        http.DefaultRequestHeaders.Authorization = new("Bearer", token);
        await using var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, McpOptions.EndpointPath),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            http,
            loggerFactory: null,
            false);

        await using var mcp = await McpClient.CreateAsync(
            transport,
            cancellationToken: CancellationToken.None);
        var result = await mcp.CallToolAsync(
            SegarisMcpToolNames.IdentityGetCurrentUser,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsError is not true);
        var content = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        using var document = JsonDocument.Parse(content.Text);
        var json = document.RootElement;
        Assert.Equal(IdentityTestServer.AdminUserName, json.GetProperty("userName").GetString());
        Assert.Contains(
            json.GetProperty("roles").EnumerateArray().Select(role => role.GetString()),
            role => role == "Admin");
    }
}

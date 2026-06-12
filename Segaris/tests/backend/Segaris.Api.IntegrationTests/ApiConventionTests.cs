using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Segaris.Api.IntegrationTests;

public sealed class ApiConventionTests
{
    [Fact]
    public async Task Pagination_returns_camel_case_metadata_and_enforces_the_maximum()
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();

        var valid = await client.GetFromJsonAsync<JsonElement>(
            "/api/platform/conventions/pagination?page=2&pageSize=100",
            CancellationToken.None);
        var invalid = await client.GetAsync(
            "/api/platform/conventions/pagination?page=0&pageSize=101",
            CancellationToken.None);
        var problem = await invalid.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(2, valid.GetProperty("page").GetInt32());
        Assert.Equal(100, valid.GetProperty("pageSize").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("request.invalid", problem.GetProperty("code").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("pageSize", out _));
    }

    [Fact]
    public async Task Pagination_rejects_unknown_sort_fields()
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/platform/conventions/pagination?sort=createdAt",
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.TryGetProperty("errors", out var errors), problem.ToString());
        Assert.True(errors.TryGetProperty("sort", out _), problem.ToString());
    }

    [Fact]
    public async Task Explicit_json_contracts_use_camel_case()
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/platform/conventions/echo",
            new { displayName = "Segaris" },
            CancellationToken.None);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        response.EnsureSuccessStatusCode();
        Assert.Equal("Segaris", body.GetProperty("displayName").GetString());
        Assert.False(body.TryGetProperty("DisplayName", out _));
    }

    [Fact]
    public async Task Malformed_json_returns_a_structured_bad_request()
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            "/api/platform/conventions/echo",
            content,
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.invalid", problem.GetProperty("code").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Oversized_api_requests_are_rejected_before_binding()
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();
        using var content = new ByteArrayContent(new byte[(1024 * 1024) + 1]);
        content.Headers.ContentType = new("application/json");

        var response = await client.PostAsync(
            "/api/platform/conventions/echo",
            content,
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("request.too_large", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Hidden_resources_return_privacy_preserving_not_found_problems()
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/platform/conventions/hidden",
            CancellationToken.None);
        var body = await response.Content.ReadAsStringAsync(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("resource.not_found", body, StringComparison.Ordinal);
        Assert.DoesNotContain("private", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unexpected_failures_are_safe_and_traceable()
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/platform/conventions/unexpected",
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("server.unexpected", problem.GetProperty("code").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("Probe failure", problem.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(401, "authentication.required")]
    [InlineData(403, "authorization.forbidden")]
    [InlineData(409, "resource.conflict")]
    [InlineData(422, "request.unprocessable")]
    [InlineData(503, "server.unavailable")]
    public async Task Standard_problem_statuses_have_stable_codes(int statusCode, string code)
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/platform/conventions/problems/{statusCode}",
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(statusCode, (int)response.StatusCode);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Request_cancellation_token_reaches_endpoint_handlers()
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<JsonElement>(
            "/api/platform/conventions/cancellation",
            CancellationToken.None);

        Assert.True(response.GetProperty("canBeCanceled").GetBoolean());
    }

    [Fact]
    public async Task Openapi_is_available_for_tests_and_routes_are_unique()
    {
        using var factory = CreateFactory("Testing");
        using var client = factory.CreateClient();

        var document = await client.GetFromJsonAsync<JsonElement>(
            "/openapi/v1.json",
            CancellationToken.None);
        var endpoints = factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => new
            {
                Pattern = endpoint.RoutePattern.RawText,
                Methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                    ?? ["ANY"],
            })
            .SelectMany(endpoint => endpoint.Methods.Select(method => $"{method} {endpoint.Pattern}"))
            .ToArray();

        Assert.StartsWith("3.1.", document.GetProperty("openapi").GetString(), StringComparison.Ordinal);
        Assert.True(document.GetProperty("paths").TryGetProperty(
            "/api/platform/conventions/pagination",
            out _));
        Assert.DoesNotContain(
            endpoints.GroupBy(value => value, StringComparer.Ordinal),
            group => group.Count() > 1);
    }

    [Fact]
    public async Task Interactive_documentation_is_not_exposed_outside_development()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var openApi = await client.GetAsync("/openapi/v1.json", CancellationToken.None);
        var scalar = await client.GetAsync("/scalar/v1", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, openApi.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, scalar.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration.AddInMemoryCollection(
                    [
                        new("Segaris:Database:Provider", "Sqlite"),
                        new("ConnectionStrings:Segaris", "Data Source=:memory:"),
                    ]);
                });
            });
    }
}

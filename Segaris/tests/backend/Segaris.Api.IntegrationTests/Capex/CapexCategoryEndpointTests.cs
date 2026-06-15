using System.Net;
using System.Net.Http.Json;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Contracts;

namespace Segaris.Api.IntegrationTests.Capex;

public sealed class CapexCategoryEndpointTests
{
    [Fact]
    public async Task Categories_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/capex/categories", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Categories_return_the_seeded_catalog_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<CapexCategoryResponse[]>(
            "/api/capex/categories",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(CapexCategoryCatalog.Categories.Count, response.Length);
        // Categories are returned in deterministic SortOrder, which mirrors the
        // frozen seed declaration order.
        Assert.Equal(
            CapexCategoryCatalog.Categories.Select(seed => seed.Name),
            response.Select(item => item.Name));
        Assert.Equal(
            Enumerable.Range(0, response.Length),
            response.Select(item => item.SortOrder));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }
}

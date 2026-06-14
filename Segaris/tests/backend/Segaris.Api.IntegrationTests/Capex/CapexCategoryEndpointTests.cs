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
    public async Task Categories_return_the_seeded_catalog_ordered_by_name()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<CapexCategoryResponse[]>(
            "/api/capex/categories",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(CapexCategoryCatalog.Categories.Count, response.Length);
        Assert.Equal(
            CapexCategoryCatalog.Categories.OrderBy(seed => seed.Name).Select(seed => seed.Code),
            response.Select(item => item.Code));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }
}

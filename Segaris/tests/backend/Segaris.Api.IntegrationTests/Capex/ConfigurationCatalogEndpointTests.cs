using System.Net;
using System.Net.Http.Json;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.IntegrationTests.Capex;

public sealed class ConfigurationCatalogEndpointTests
{
    [Theory]
    [InlineData("/api/configuration/suppliers")]
    [InlineData("/api/configuration/cost-centers")]
    [InlineData("/api/configuration/currencies")]
    public async Task Catalog_endpoints_require_authentication(string route)
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync(route, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Suppliers_return_the_seeded_catalog()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<SupplierResponse[]>(
            "/api/configuration/suppliers",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(
            ConfigurationCatalog.Suppliers.OrderBy(seed => seed.Name).Select(seed => seed.Code),
            response.Select(item => item.Code));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }

    [Fact]
    public async Task Cost_centers_return_the_seeded_catalog()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<CostCenterResponse[]>(
            "/api/configuration/cost-centers",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(
            ConfigurationCatalog.CostCenters.OrderBy(seed => seed.Name).Select(seed => seed.Code),
            response.Select(item => item.Code));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }

    [Fact]
    public async Task Currencies_return_the_seeded_catalog()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<CurrencyResponse[]>(
            "/api/configuration/currencies",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(
            ConfigurationCatalog.Currencies.OrderBy(seed => seed.Name).Select(seed => seed.Code),
            response.Select(item => item.Code));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }
}

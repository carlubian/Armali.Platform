using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Configuration;

internal static class ConfigurationEndpoints
{
    public static void MapConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("configuration", ConfigurationApiRoutes.Tag)
            .RequireAuthorization();

        group.MapGet("/suppliers", ListSuppliersAsync)
            .WithName("ListConfigurationSuppliers")
            .WithSummary("Returns the shared supplier catalog")
            .Produces<IReadOnlyList<SupplierResponse>>();

        group.MapGet("/cost-centers", ListCostCentersAsync)
            .WithName("ListConfigurationCostCenters")
            .WithSummary("Returns the shared cost-center catalog")
            .Produces<IReadOnlyList<CostCenterResponse>>();

        group.MapGet("/currencies", ListCurrenciesAsync)
            .WithName("ListConfigurationCurrencies")
            .WithSummary("Returns the shared currency catalog")
            .Produces<IReadOnlyList<CurrencyResponse>>();
    }

    private static async Task<IResult> ListSuppliersAsync(
        IConfigurationCatalog catalog,
        CancellationToken cancellationToken)
    {
        var values = await catalog.ListSuppliersAsync(cancellationToken);
        return TypedResults.Ok(values
            .Select(value => new SupplierResponse(value.Id, value.Code, value.Name))
            .ToArray());
    }

    private static async Task<IResult> ListCostCentersAsync(
        IConfigurationCatalog catalog,
        CancellationToken cancellationToken)
    {
        var values = await catalog.ListCostCentersAsync(cancellationToken);
        return TypedResults.Ok(values
            .Select(value => new CostCenterResponse(value.Id, value.Code, value.Name))
            .ToArray());
    }

    private static async Task<IResult> ListCurrenciesAsync(
        IConfigurationCatalog catalog,
        CancellationToken cancellationToken)
    {
        var values = await catalog.ListCurrenciesAsync(cancellationToken);
        return TypedResults.Ok(values
            .Select(value => new CurrencyResponse(value.Id, value.Code, value.Name))
            .ToArray());
    }
}

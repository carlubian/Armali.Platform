using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

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

        MapManagement<SupplierResponse>(group, "/suppliers", "Supplier", CreateSupplierAsync, UpdateSupplierAsync, MoveSupplierAsync, SupplierImpactAsync, DeleteSupplierAsync, ReplaceAndDeleteSupplierAsync);
        MapManagement<CostCenterResponse>(group, "/cost-centers", "CostCenter", CreateCostCenterAsync, UpdateCostCenterAsync, MoveCostCenterAsync, CostCenterImpactAsync, DeleteCostCenterAsync, ReplaceAndDeleteCostCenterAsync);

        var currencies = group.MapGroup("/currencies").RequireAuthorization(IdentityPolicies.Admin);
        currencies.MapPost("", CreateCurrencyAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateConfigurationCurrency").WithSummary("Creates a currency at the end of the catalog").Produces<CurrencyResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        currencies.MapPut(ConfigurationApiRoutes.ById, UpdateCurrencyAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateConfigurationCurrency").WithSummary("Updates a currency name and code").Produces<CurrencyResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        currencies.MapPost(ConfigurationApiRoutes.Move, MoveCurrencyAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveConfigurationCurrency").WithSummary("Moves a currency one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        currencies.MapGet(ConfigurationApiRoutes.DeletionImpact, CurrencyImpactAsync).WithName("GetConfigurationCurrencyDeletionImpact").WithSummary("Returns privacy-neutral currency deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        currencies.MapDelete(ConfigurationApiRoutes.ById, DeleteCurrencyAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteConfigurationCurrency").WithSummary("Deletes an unreferenced currency").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        currencies.MapPost(ConfigurationApiRoutes.ReplaceAndDelete, ReplaceAndDeleteCurrencyAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteConfigurationCurrency").WithSummary("Converts referenced entries to another currency and deletes this one atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapManagement<TResponse>(
        RouteGroupBuilder root,
        string path,
        string operationName,
        Delegate create,
        Delegate update,
        Delegate move,
        Delegate impact,
        Delegate delete,
        Delegate replaceAndDelete)
    {
        var group = root.MapGroup(path).RequireAuthorization(IdentityPolicies.Admin);
        group.MapPost("", create).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName($"CreateConfiguration{operationName}").WithSummary($"Creates a {operationName} value at the end of the catalog").Produces<TResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut(ConfigurationApiRoutes.ById, update).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName($"UpdateConfiguration{operationName}").WithSummary($"Updates a {operationName} value").Produces<TResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost(ConfigurationApiRoutes.Move, move).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName($"MoveConfiguration{operationName}").WithSummary($"Moves a {operationName} value one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet(ConfigurationApiRoutes.DeletionImpact, impact).WithName($"GetConfiguration{operationName}DeletionImpact").WithSummary($"Returns privacy-neutral {operationName} deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete(ConfigurationApiRoutes.ById, delete).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName($"DeleteConfiguration{operationName}").WithSummary($"Deletes an unreferenced {operationName} value").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost(ConfigurationApiRoutes.ReplaceAndDelete, replaceAndDelete).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName($"ReplaceAndDeleteConfiguration{operationName}").WithSummary($"Migrates references and deletes a {operationName} value atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static UserId Actor(ICurrentUser currentUser) => currentUser.UserId ?? throw ConfigurationProblem.NotFound();
    private static CatalogMoveDirection Direction(CatalogMoveRequest request) => CatalogMoveDirections.TryParse(request.Direction, out var direction) ? direction : throw ConfigurationProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateSupplierAsync(CatalogItemRequest request, ConfigurationCatalogManagementService service, ICurrentUser user, CancellationToken token) { var value = await service.CreateSupplierAsync(request, Actor(user), token); return TypedResults.Created($"/api/configuration/suppliers/{value.Id}", value); }
    private static async Task<IResult> UpdateSupplierAsync(int id, CatalogItemRequest request, ConfigurationCatalogManagementService service, ICurrentUser user, CancellationToken token) => TypedResults.Ok(await service.UpdateSupplierAsync(id, request, Actor(user), token));
    private static async Task<IResult> MoveSupplierAsync(int id, CatalogMoveRequest request, ConfigurationCatalogManagementService service, CancellationToken token) { await service.MoveSupplierAsync(id, Direction(request), token); return TypedResults.NoContent(); }
    private static async Task<IResult> SupplierImpactAsync(int id, ConfigurationCatalogManagementService service, CancellationToken token) => TypedResults.Ok(await service.SupplierImpactAsync(id, token));
    private static async Task<IResult> DeleteSupplierAsync(int id, ConfigurationCatalogManagementService service, CancellationToken token) { await service.DeleteSupplierAsync(id, token); return TypedResults.NoContent(); }
    private static async Task<IResult> ReplaceAndDeleteSupplierAsync(int id, CatalogReplacementRequest request, ConfigurationCatalogManagementService service, ICurrentUser user, CancellationToken token) { await service.ReplaceAndDeleteSupplierAsync(id, request, Actor(user), token); return TypedResults.NoContent(); }

    private static async Task<IResult> CreateCostCenterAsync(CatalogItemRequest request, ConfigurationCatalogManagementService service, ICurrentUser user, CancellationToken token) { var value = await service.CreateCostCenterAsync(request, Actor(user), token); return TypedResults.Created($"/api/configuration/cost-centers/{value.Id}", value); }
    private static async Task<IResult> UpdateCostCenterAsync(int id, CatalogItemRequest request, ConfigurationCatalogManagementService service, ICurrentUser user, CancellationToken token) => TypedResults.Ok(await service.UpdateCostCenterAsync(id, request, Actor(user), token));
    private static async Task<IResult> MoveCostCenterAsync(int id, CatalogMoveRequest request, ConfigurationCatalogManagementService service, CancellationToken token) { await service.MoveCostCenterAsync(id, Direction(request), token); return TypedResults.NoContent(); }
    private static async Task<IResult> CostCenterImpactAsync(int id, ConfigurationCatalogManagementService service, CancellationToken token) => TypedResults.Ok(await service.CostCenterImpactAsync(id, token));
    private static async Task<IResult> DeleteCostCenterAsync(int id, ConfigurationCatalogManagementService service, CancellationToken token) { await service.DeleteCostCenterAsync(id, token); return TypedResults.NoContent(); }
    private static async Task<IResult> ReplaceAndDeleteCostCenterAsync(int id, CatalogReplacementRequest request, ConfigurationCatalogManagementService service, ICurrentUser user, CancellationToken token) { await service.ReplaceAndDeleteCostCenterAsync(id, request, Actor(user), token); return TypedResults.NoContent(); }

    private static async Task<IResult> CreateCurrencyAsync(CurrencyItemRequest request, ConfigurationCatalogManagementService service, ICurrentUser user, CancellationToken token) { var value = await service.CreateCurrencyAsync(request, Actor(user), token); return TypedResults.Created($"/api/configuration/currencies/{value.Id}", value); }
    private static async Task<IResult> UpdateCurrencyAsync(int id, CurrencyItemRequest request, ConfigurationCatalogManagementService service, ICurrentUser user, CancellationToken token) => TypedResults.Ok(await service.UpdateCurrencyAsync(id, request, Actor(user), token));
    private static async Task<IResult> MoveCurrencyAsync(int id, CatalogMoveRequest request, ConfigurationCatalogManagementService service, CancellationToken token) { await service.MoveCurrencyAsync(id, Direction(request), token); return TypedResults.NoContent(); }
    private static async Task<IResult> CurrencyImpactAsync(int id, ConfigurationCatalogManagementService service, CancellationToken token) => TypedResults.Ok(await service.CurrencyImpactAsync(id, token));
    private static async Task<IResult> DeleteCurrencyAsync(int id, ConfigurationCatalogManagementService service, CancellationToken token) { await service.DeleteCurrencyAsync(id, token); return TypedResults.NoContent(); }
    private static async Task<IResult> ReplaceAndDeleteCurrencyAsync(int id, CatalogReplacementRequest request, ConfigurationCatalogManagementService service, ICurrentUser user, CancellationToken token) { await service.ReplaceAndDeleteCurrencyAsync(id, request, Actor(user), token); return TypedResults.NoContent(); }

    private static async Task<IResult> ListSuppliersAsync(
        IConfigurationCatalog catalog,
        CancellationToken cancellationToken)
    {
        var values = await catalog.ListSuppliersAsync(cancellationToken);
        return TypedResults.Ok(values
            .Select(value => new SupplierResponse(value.Id, value.Name, value.SortOrder))
            .ToArray());
    }

    private static async Task<IResult> ListCostCentersAsync(
        IConfigurationCatalog catalog,
        CancellationToken cancellationToken)
    {
        var values = await catalog.ListCostCentersAsync(cancellationToken);
        return TypedResults.Ok(values
            .Select(value => new CostCenterResponse(value.Id, value.Name, value.SortOrder))
            .ToArray());
    }

    private static async Task<IResult> ListCurrenciesAsync(
        IConfigurationCatalog catalog,
        CancellationToken cancellationToken)
    {
        var values = await catalog.ListCurrenciesAsync(cancellationToken);
        return TypedResults.Ok(values
            .Select(value => new CurrencyResponse(value.Id, value.Code, value.Name, value.SortOrder, value.ExchangeRateToEur))
            .ToArray());
    }
}

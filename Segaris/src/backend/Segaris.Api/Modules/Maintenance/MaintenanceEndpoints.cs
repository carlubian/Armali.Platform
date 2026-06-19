using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Mutations;
using Segaris.Api.Modules.Maintenance.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Maintenance;

/// <summary>
/// Maps the Maintenance HTTP surface. Wave 1 exposes the module-owned
/// <c>MaintenanceType</c> catalogue read and the administrator-only catalogue
/// management routes surfaced through Configuration; later waves add the task and
/// attachment routes. State-changing routes carry antiforgery protection and never
/// expose EF Core entities.
/// </summary>
internal static class MaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("maintenance", MaintenanceApiRoutes.Tag)
            .RequireAuthorization();

        MapTypeEndpoints(group);

        return endpoints;
    }

    private static void MapTypeEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/types", ListTypesAsync)
            .WithName("ListMaintenanceTypes")
            .WithSummary("Returns the Maintenance type catalogue")
            .Produces<IReadOnlyList<MaintenanceTypeResponse>>();

        var types = group.MapGroup("/types").RequireAuthorization(IdentityPolicies.Admin);
        types.MapPost("", CreateTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateMaintenanceType").WithSummary("Creates a maintenance type at the end of the catalogue").Produces<MaintenanceTypeResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        types.MapPut(MaintenanceApiRoutes.TypeById, UpdateTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateMaintenanceType").WithSummary("Updates a maintenance type").Produces<MaintenanceTypeResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        types.MapPost(MaintenanceApiRoutes.TypeMove, MoveTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveMaintenanceType").WithSummary("Moves a maintenance type one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        types.MapGet(MaintenanceApiRoutes.TypeDeletionImpact, TypeImpactAsync).WithName("GetMaintenanceTypeDeletionImpact").WithSummary("Returns privacy-neutral maintenance type deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        types.MapDelete(MaintenanceApiRoutes.TypeById, DeleteTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteMaintenanceType").WithSummary("Deletes an unreferenced maintenance type").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        types.MapPost(MaintenanceApiRoutes.TypeReplaceAndDelete, ReplaceAndDeleteTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteMaintenanceType").WithSummary("Migrates references and deletes a maintenance type atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListTypesAsync(MaintenanceTypeReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListAsync(cancellationToken));

    private static UserId CatalogActor(ICurrentUser currentUser) => currentUser.UserId ?? throw MaintenanceTypeProblem.NotFound();

    private static CatalogMoveDirection TypeDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw MaintenanceTypeProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateTypeAsync(CatalogItemRequest request, MaintenanceTypeManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/maintenance/types/{value.Id}", value);
    }

    private static async Task<IResult> UpdateTypeAsync(int typeId, CatalogItemRequest request, MaintenanceTypeManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(typeId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveTypeAsync(int typeId, CatalogMoveRequest request, MaintenanceTypeManagementService service, CancellationToken token)
    {
        await service.MoveAsync(typeId, TypeDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> TypeImpactAsync(int typeId, MaintenanceTypeManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(typeId, token));

    private static async Task<IResult> DeleteTypeAsync(int typeId, MaintenanceTypeManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(typeId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteTypeAsync(int typeId, CatalogReplacementRequest request, MaintenanceTypeManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(typeId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }
}

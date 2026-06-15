using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Mutations;
using Segaris.Api.Modules.Opex.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Opex;

/// <summary>
/// Maps the Opex HTTP surface. Wave 1 exposes the category catalog read and the
/// administrator-only category management routes; Wave 2 adds the paginated
/// contracts list and contract detail reads frozen in <see cref="OpexApiRoutes"/>.
/// The occurrence and attachment routes are added by later Waves. State-changing
/// routes carry antiforgery protection and never expose EF Core entities.
/// </summary>
internal static class OpexEndpoints
{
    public static void MapOpexEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("opex", OpexApiRoutes.Tag)
            .RequireAuthorization();

        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListOpexCategories")
            .WithSummary("Returns the Opex category catalog")
            .Produces<IReadOnlyList<OpexCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateOpexCategory").WithSummary("Creates a category at the end of the catalog").Produces<OpexCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(OpexApiRoutes.CategoryById, UpdateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateOpexCategory").WithSummary("Updates an Opex category").Produces<OpexCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(OpexApiRoutes.CategoryMove, MoveCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveOpexCategory").WithSummary("Moves an Opex category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(OpexApiRoutes.CategoryDeletionImpact, CategoryImpactAsync).WithName("GetOpexCategoryDeletionImpact").WithSummary("Returns privacy-neutral category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(OpexApiRoutes.CategoryById, DeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteOpexCategory").WithSummary("Deletes an unreferenced Opex category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(OpexApiRoutes.CategoryReplaceAndDelete, ReplaceAndDeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteOpexCategory").WithSummary("Migrates references and deletes an Opex category atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/contracts", ListContractsAsync)
            .WithName("ListOpexContracts")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible contracts with current-year realized amounts")
            .Produces<PaginatedResponse<OpexContractSummaryResponse>>();

        group.MapGet("/contracts/{contractId:int}", GetContractAsync)
            .WithName("GetOpexContract")
            .WithSummary("Returns the detail of an accessible contract with its attachments")
            .Produces<OpexContractResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListContractsAsync(
        [AsParameters] OpexContractListQuery query,
        OpexReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var pagination = query.ToPagination();
        var sort = query.ToSort();
        var filter = query.ToFilter();

        var result = await read.ListContractsAsync(filter, pagination, sort, userId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetContractAsync(
        int contractId,
        OpexReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var contract = await read.GetContractAsync(contractId, userId, cancellationToken);
        if (contract is null)
        {
            throw OpexProblem.ContractNotFound();
        }

        return TypedResults.Ok(contract);
    }

    private static async Task<IResult> ListCategoriesAsync(
        OpexReadService read,
        CancellationToken cancellationToken)
    {
        var categories = await read.ListCategoriesAsync(cancellationToken);
        return TypedResults.Ok(categories);
    }

    private static UserId CategoryActor(ICurrentUser currentUser) => currentUser.UserId ?? throw OpexCategoryProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw OpexCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(CatalogItemRequest request, OpexCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CategoryActor(user), token);
        return TypedResults.Created($"/api/opex/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(int categoryId, CatalogItemRequest request, OpexCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CategoryActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(int categoryId, CatalogMoveRequest request, OpexCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(int categoryId, OpexCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(int categoryId, OpexCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteCategoryAsync(int categoryId, CatalogReplacementRequest request, OpexCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, CategoryActor(user), token);
        return TypedResults.NoContent();
    }
}

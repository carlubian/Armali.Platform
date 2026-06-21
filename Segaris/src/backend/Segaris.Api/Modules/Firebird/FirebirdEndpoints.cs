using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Modules.Firebird.Mutations;
using Segaris.Api.Modules.Firebird.Queries;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Firebird;

internal static class FirebirdEndpoints
{
    public static IEndpointRouteBuilder MapFirebirdEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup(FirebirdApiRoutes.People, FirebirdApiRoutes.Tag)
            .RequireAuthorization();

        MapPersonEndpoints(group);
        MapCategoryEndpoints(group);
        MapPlatformEndpoints(group);

        return endpoints;
    }

    private static void MapPersonEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("", ListPeopleAsync)
            .WithName("ListFirebirdPeople")
            .WithSummary("Returns a paginated, filtered, and sorted gallery of accessible Firebird people")
            .Produces<PaginatedResponse<PersonSummaryResponse>>();
        group.MapPost("", CreatePersonAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateFirebirdPerson")
            .WithSummary("Creates a Firebird person")
            .Produces<PersonResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet(FirebirdApiRoutes.PersonById, GetPersonAsync)
            .WithName("GetFirebirdPerson")
            .WithSummary("Returns the detail of an accessible Firebird person")
            .Produces<PersonResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut(FirebirdApiRoutes.PersonById, UpdatePersonAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateFirebirdPerson")
            .WithSummary("Replaces an accessible Firebird person")
            .Produces<PersonResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete(FirebirdApiRoutes.PersonById, DeletePersonAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteFirebirdPerson")
            .WithSummary("Deletes an accessible Firebird person")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListPersonCategories")
            .WithSummary("Returns the Firebird person category catalogue")
            .Produces<IReadOnlyList<PersonCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreatePersonCategory").WithSummary("Creates a person category at the end of the catalogue").Produces<PersonCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(FirebirdApiRoutes.CategoryById, UpdateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdatePersonCategory").WithSummary("Updates a person category").Produces<PersonCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(FirebirdApiRoutes.CategoryMove, MoveCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MovePersonCategory").WithSummary("Moves a person category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(FirebirdApiRoutes.CategoryDeletionImpact, CategoryImpactAsync).WithName("GetPersonCategoryDeletionImpact").WithSummary("Returns privacy-neutral person category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(FirebirdApiRoutes.CategoryById, DeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeletePersonCategory").WithSummary("Deletes an unreferenced person category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(FirebirdApiRoutes.CategoryReplaceAndDelete, ReplaceAndDeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeletePersonCategory").WithSummary("Migrates references and deletes a person category atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapPlatformEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/platforms", ListPlatformsAsync)
            .WithName("ListUsernamePlatforms")
            .WithSummary("Returns the Firebird username platform catalogue")
            .Produces<IReadOnlyList<UsernamePlatformResponse>>();

        var platforms = group.MapGroup("/platforms").RequireAuthorization(IdentityPolicies.Admin);
        platforms.MapPost("", CreatePlatformAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateUsernamePlatform").WithSummary("Creates a username platform at the end of the catalogue").Produces<UsernamePlatformResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        platforms.MapPut(FirebirdApiRoutes.PlatformById, UpdatePlatformAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateUsernamePlatform").WithSummary("Updates a username platform").Produces<UsernamePlatformResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        platforms.MapPost(FirebirdApiRoutes.PlatformMove, MovePlatformAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveUsernamePlatform").WithSummary("Moves a username platform one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        platforms.MapGet(FirebirdApiRoutes.PlatformDeletionImpact, PlatformImpactAsync).WithName("GetUsernamePlatformDeletionImpact").WithSummary("Returns privacy-neutral username platform deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        platforms.MapDelete(FirebirdApiRoutes.PlatformById, DeletePlatformAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteUsernamePlatform").WithSummary("Deletes an unreferenced username platform").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        platforms.MapPost(FirebirdApiRoutes.PlatformReplaceAndDelete, ReplaceAndDeletePlatformAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteUsernamePlatform").WithSummary("Migrates references and deletes a username platform atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListCategoriesAsync(FirebirdCatalogReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListCategoriesAsync(cancellationToken));

    private static async Task<IResult> ListPlatformsAsync(FirebirdCatalogReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListPlatformsAsync(cancellationToken));

    private static async Task<IResult> ListPeopleAsync(
        [AsParameters] FirebirdPersonListQuery query,
        FirebirdPersonReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListPeopleAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetPersonAsync(
        int personId,
        FirebirdPersonReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var person = await read.GetPersonAsync(personId, userId, cancellationToken);
        if (person is null)
        {
            throw FirebirdPersonProblem.NotFound();
        }

        return TypedResults.Ok(person);
    }

    private static async Task<IResult> CreatePersonAsync(
        CreatePersonRequest request,
        FirebirdPersonWriteService write,
        FirebirdPersonReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int personId;
        try
        {
            personId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (FirebirdValidationException exception)
        {
            throw FirebirdPersonProblem.From(exception);
        }

        var created = await read.GetPersonAsync(personId, userId, cancellationToken);
        return TypedResults.Created($"/api/people/{personId}", created);
    }

    private static async Task<IResult> UpdatePersonAsync(
        int personId,
        UpdatePersonRequest request,
        FirebirdPersonWriteService write,
        FirebirdPersonReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool updated;
        try
        {
            updated = await write.UpdateAsync(personId, request, userId, cancellationToken);
        }
        catch (FirebirdValidationException exception)
        {
            throw FirebirdPersonProblem.From(exception);
        }

        if (!updated)
        {
            throw FirebirdPersonProblem.NotFound();
        }

        var person = await read.GetPersonAsync(personId, userId, cancellationToken);
        return TypedResults.Ok(person);
    }

    private static async Task<IResult> DeletePersonAsync(
        int personId,
        FirebirdPersonWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(personId, userId, cancellationToken);
        if (!deleted)
        {
            throw FirebirdPersonProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static UserId CatalogActor(ICurrentUser currentUser) => currentUser.UserId ?? throw PersonCategoryProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw PersonCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection PlatformDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw UsernamePlatformProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(PersonCategoryRequest request, PersonCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/people/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(int categoryId, PersonCategoryRequest request, PersonCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(int categoryId, CatalogMoveRequest request, PersonCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(int categoryId, PersonCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(int categoryId, PersonCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteCategoryAsync(int categoryId, CatalogReplacementRequest request, PersonCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreatePlatformAsync(UsernamePlatformRequest request, UsernamePlatformManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/people/platforms/{value.Id}", value);
    }

    private static async Task<IResult> UpdatePlatformAsync(int platformId, UsernamePlatformRequest request, UsernamePlatformManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(platformId, request, CatalogActor(user), token));

    private static async Task<IResult> MovePlatformAsync(int platformId, CatalogMoveRequest request, UsernamePlatformManagementService service, CancellationToken token)
    {
        await service.MoveAsync(platformId, PlatformDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> PlatformImpactAsync(int platformId, UsernamePlatformManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(platformId, token));

    private static async Task<IResult> DeletePlatformAsync(int platformId, UsernamePlatformManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(platformId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeletePlatformAsync(int platformId, CatalogReplacementRequest request, UsernamePlatformManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(platformId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }
}

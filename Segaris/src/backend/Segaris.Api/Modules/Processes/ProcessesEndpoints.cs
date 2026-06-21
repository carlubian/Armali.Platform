using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Api.Modules.Processes.Mutations;
using Segaris.Api.Modules.Processes.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Processes;

/// <summary>
/// Maps the Processes HTTP surface. State-changing routes carry antiforgery protection
/// and never expose EF Core entities.
/// </summary>
internal static class ProcessesEndpoints
{
    public static IEndpointRouteBuilder MapProcessesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("processes", ProcessesApiRoutes.Tag)
            .RequireAuthorization();

        MapProcessEndpoints(group);
        MapCategoryEndpoints(group);

        return endpoints;
    }

    private static void MapProcessEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("", ListProcessesAsync)
            .WithName("ListProcesses")
            .WithSummary("Returns a paginated, filtered, and sorted table of accessible Processes")
            .Produces<PaginatedResponse<ProcessSummaryResponse>>();
        group.MapPost("", CreateProcessAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateProcess")
            .WithSummary("Creates a process")
            .Produces<ProcessResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet(ProcessesApiRoutes.ProcessById, GetProcessAsync)
            .WithName("GetProcess")
            .WithSummary("Returns the detail of an accessible process")
            .Produces<ProcessResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut(ProcessesApiRoutes.ProcessById, UpdateProcessAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateProcess")
            .WithSummary("Replaces an accessible process")
            .Produces<ProcessResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete(ProcessesApiRoutes.ProcessById, DeleteProcessAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteProcess")
            .WithSummary("Deletes an accessible process")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost(ProcessesApiRoutes.ProcessCancel, CancelProcessAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CancelProcess")
            .WithSummary("Sets the terminal Cancelled override on an accessible process")
            .Produces<ProcessResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost(ProcessesApiRoutes.ProcessReopen, ReopenProcessAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ReopenProcess")
            .WithSummary("Clears the terminal Cancelled override on an accessible process")
            .Produces<ProcessResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet(ProcessesApiRoutes.ProcessSteps, ListStepsAsync)
            .WithName("ListProcessSteps")
            .WithSummary("Returns the ordered step list of an accessible process")
            .Produces<IReadOnlyList<StepResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut(ProcessesApiRoutes.ProcessSteps, UpdateStepsAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateProcessSteps")
            .WithSummary("Replaces the ordered step list while preserving state by identity")
            .Produces<ProcessResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost(ProcessesApiRoutes.StepComplete, CompleteStepAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CompleteProcessStep")
            .WithSummary("Completes the frontier step of an accessible process")
            .Produces<ProcessResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost(ProcessesApiRoutes.StepSkip, SkipStepAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("SkipProcessStep")
            .WithSummary("Skips an optional frontier step of an accessible process")
            .Produces<ProcessResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost(ProcessesApiRoutes.StepUndo, UndoStepAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UndoProcessStep")
            .WithSummary("Returns the most recently resolved step to pending")
            .Produces<ProcessResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListProcessCategories")
            .WithSummary("Returns the Processes category catalogue")
            .Produces<IReadOnlyList<ProcessCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateProcessCategory").WithSummary("Creates a process category at the end of the catalogue").Produces<ProcessCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(ProcessesApiRoutes.CategoryById, UpdateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateProcessCategory").WithSummary("Updates a process category").Produces<ProcessCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(ProcessesApiRoutes.CategoryMove, MoveCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveProcessCategory").WithSummary("Moves a process category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(ProcessesApiRoutes.CategoryDeletionImpact, CategoryImpactAsync).WithName("GetProcessCategoryDeletionImpact").WithSummary("Returns privacy-neutral process category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(ProcessesApiRoutes.CategoryById, DeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteProcessCategory").WithSummary("Deletes an unreferenced process category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(ProcessesApiRoutes.CategoryReplaceAndDelete, ReplaceAndDeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteProcessCategory").WithSummary("Migrates references and deletes a process category atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListCategoriesAsync(ProcessCategoryReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListAsync(cancellationToken));

    private static async Task<IResult> ListProcessesAsync(
        [AsParameters] ProcessListQuery query,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetProcessAsync(
        int processId,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var process = await read.GetAsync(processId, userId, cancellationToken);
        if (process is null)
        {
            throw ProcessProblem.NotFound();
        }

        return TypedResults.Ok(process);
    }

    private static async Task<IResult> CreateProcessAsync(
        CreateProcessRequest request,
        ProcessWriteService write,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int processId;
        try
        {
            processId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (ProcessesValidationException exception)
        {
            throw ProcessProblem.From(exception);
        }

        var created = await read.GetAsync(processId, userId, cancellationToken);
        return TypedResults.Created($"/api/processes/{processId}", created);
    }

    private static async Task<IResult> UpdateProcessAsync(
        int processId,
        UpdateProcessRequest request,
        ProcessWriteService write,
        ProcessReadService read,
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
            updated = await write.UpdateAsync(processId, request, userId, cancellationToken);
        }
        catch (ProcessesValidationException exception)
        {
            throw ProcessProblem.From(exception);
        }

        if (!updated)
        {
            throw ProcessProblem.NotFound();
        }

        var process = await read.GetAsync(processId, userId, cancellationToken);
        return TypedResults.Ok(process);
    }

    private static async Task<IResult> DeleteProcessAsync(
        int processId,
        ProcessWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(processId, userId, cancellationToken);
        if (!deleted)
        {
            throw ProcessProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> CancelProcessAsync(
        int processId,
        ProcessWriteService write,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var cancelled = await write.CancelAsync(processId, userId, cancellationToken);
        if (!cancelled)
        {
            throw ProcessProblem.NotFound();
        }

        return TypedResults.Ok(await read.GetAsync(processId, userId, cancellationToken));
    }

    private static async Task<IResult> ReopenProcessAsync(
        int processId,
        ProcessWriteService write,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var reopened = await write.ReopenAsync(processId, userId, cancellationToken);
        if (!reopened)
        {
            throw ProcessProblem.NotFound();
        }

        return TypedResults.Ok(await read.GetAsync(processId, userId, cancellationToken));
    }

    private static async Task<IResult> ListStepsAsync(
        int processId,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var steps = await read.GetStepsAsync(processId, userId, cancellationToken);
        if (steps is null)
        {
            throw ProcessProblem.NotFound();
        }

        return TypedResults.Ok(steps);
    }

    private static async Task<IResult> UpdateStepsAsync(
        int processId,
        UpdateStepListRequest request,
        ProcessWriteService write,
        ProcessReadService read,
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
            updated = await write.UpdateStepsAsync(processId, request, userId, cancellationToken);
        }
        catch (ProcessesValidationException exception)
        {
            throw ProcessProblem.From(exception);
        }

        if (!updated)
        {
            throw ProcessProblem.NotFound();
        }

        return TypedResults.Ok(await read.GetAsync(processId, userId, cancellationToken));
    }

    private static async Task<IResult> CompleteStepAsync(
        int processId,
        int stepId,
        ProcessWriteService write,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        await ApplyStepActionAsync(
            processId,
            stepId,
            write.CompleteStepAsync,
            read,
            currentUser,
            cancellationToken);

    private static async Task<IResult> SkipStepAsync(
        int processId,
        int stepId,
        ProcessWriteService write,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        await ApplyStepActionAsync(
            processId,
            stepId,
            write.SkipStepAsync,
            read,
            currentUser,
            cancellationToken);

    private static async Task<IResult> UndoStepAsync(
        int processId,
        int stepId,
        ProcessWriteService write,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        await ApplyStepActionAsync(
            processId,
            stepId,
            write.UndoStepAsync,
            read,
            currentUser,
            cancellationToken);

    private static async Task<IResult> ApplyStepActionAsync(
        int processId,
        int stepId,
        Func<int, int, UserId, CancellationToken, Task<bool>> action,
        ProcessReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool applied;
        try
        {
            applied = await action(processId, stepId, userId, cancellationToken);
        }
        catch (ProcessesValidationException exception)
        {
            throw ProcessProblem.From(exception);
        }

        if (!applied)
        {
            throw ProcessProblem.NotFound();
        }

        return TypedResults.Ok(await read.GetAsync(processId, userId, cancellationToken));
    }

    private static UserId CatalogActor(ICurrentUser currentUser) => currentUser.UserId ?? throw ProcessCategoryProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw ProcessCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(ProcessCategoryRequest request, ProcessCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/processes/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(int categoryId, ProcessCategoryRequest request, ProcessCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(int categoryId, CatalogMoveRequest request, ProcessCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(int categoryId, ProcessCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(int categoryId, ProcessCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteCategoryAsync(int categoryId, CatalogReplacementRequest request, ProcessCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }
}
